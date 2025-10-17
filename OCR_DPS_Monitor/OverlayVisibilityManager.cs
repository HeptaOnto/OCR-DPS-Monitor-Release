using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OCR_DPS_Monitor
{
    public class OverlayVisibilityManager
    {
        private IntPtr _targetWindowHandle;
        private readonly MainWindow _mainWindow;
        private readonly OverlayBlocksForm _overlayForm;
        private IntPtr _hookHandle;
        private WinEventDelegate _winEventDelegate;

        private volatile bool _isDisposed = false;
        private readonly object _disposeLock = new object();

        public OverlayVisibilityManager(MainWindow mainForm, OverlayBlocksForm overlayForm)
        {
            _mainWindow = mainForm;
            _overlayForm = overlayForm;
            _winEventDelegate = new WinEventDelegate(WinEventProc); // Сохраняем делегат
        }

        public void Initialize(IntPtr targetWindowHandle)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(OverlayVisibilityManager));

            Debug.WriteLine($"Initialize: Current={_targetWindowHandle}, New={targetWindowHandle}");

            // Если handle не изменился - ничего не делаем
            if (_targetWindowHandle == targetWindowHandle)
            {
                Debug.WriteLine("Handle unchanged - skipping");
                return;
            }

            // Всегда снимаем старый хук при изменении handle
            if (_hookHandle != IntPtr.Zero)
            {
                Debug.WriteLine($"Removing hook for old handle: {_targetWindowHandle}");
                UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }

            _targetWindowHandle = targetWindowHandle;

            // Устанавливаем новый хук только для валидного handle
            if (targetWindowHandle != IntPtr.Zero)
            {
                Debug.WriteLine($"Setting hook for new handle: {_targetWindowHandle}");
                SetWinEventHook();
            }
        }
        public void Stop()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                Debug.WriteLine($"Stopping hook for handle: {_targetWindowHandle}");
                UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            _targetWindowHandle = IntPtr.Zero;
        }

        private void SetWinEventHook()
        {
            if (_isDisposed || _targetWindowHandle == IntPtr.Zero)
                return;

            try
            {
                _hookHandle = SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND,
                    EVENT_SYSTEM_MINIMIZESTART,
                    IntPtr.Zero,
                    _winEventDelegate,
                    0, 0,
                    WINEVENT_OUTOFCONTEXT);

                if (_hookHandle == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не падаем
                System.Diagnostics.Debug.WriteLine($"Failed to set window hook: {ex.Message}");
            }
        }

        private bool IsChildOfOverlay(IntPtr hwnd)
        {
            IntPtr parent = GetParent(hwnd);
            while (parent != IntPtr.Zero)
            {
                if (parent == _overlayForm.Handle)
                    return true;
                parent = GetParent(parent);
            }
            return false;
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (_isDisposed) return;

            try
            {
                // Игнорируем события, связанные с самим оверлеем
                if (hwnd == _overlayForm.Handle || IsChildOfOverlay(hwnd))
                    return;

                // Только события смены активного окна и минимизации
                if (eventType == EVENT_SYSTEM_FOREGROUND ||
                    eventType == EVENT_SYSTEM_MINIMIZESTART)
                {
                    UpdateOverlayVisibility();
                }
            }
            catch (Exception ex)
            {
                // Защита от исключений в callback-е
                System.Diagnostics.Debug.WriteLine($"Error in WinEventProc: {ex.Message}");
            }
        }

        private bool IsWindowActive(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            IntPtr foregroundWindow = GetForegroundWindow();
            bool isVisible = IsWindowVisible(hWnd);
            bool isIconic = IsIconic(hWnd);
            bool isForeground = (foregroundWindow == hWnd);

            bool result = isForeground && isVisible && !isIconic;

            //Debug.WriteLine($"IsWindowActive - Handle: {hWnd}, Foreground: {foregroundWindow}, " +
                           //$"Visible: {isVisible}, Minimized: {isIconic}, Result: {result}");
            return result;
        }
        //private bool ShouldCheckVisibility(IntPtr hwnd, uint eventType)
        //{
        //    if (_isDisposed || _targetWindowHandle == IntPtr.Zero)
        //        return false;

        //    // Всегда проверяем если событие связано с нашим целевым окном
        //    if (hwnd == _targetWindowHandle)
        //        return true;

        //    // Если фокус перешел на другое окно - проверяем потерял ли наше окно фокус
        //    if (eventType == EVENT_SYSTEM_FOREGROUND)
        //    {
        //        // Получаем текущее активное окно
        //        IntPtr foregroundWindow = GetForegroundWindow();

        //        // Если активное окно - не наше целевое, нужно скрыть оверлей
        //        if (foregroundWindow != _targetWindowHandle)
        //            return true;

        //        // Если активное окно стало нашим целевым - показать оверлей
        //        if (foregroundWindow == _targetWindowHandle)
        //            return true;
        //    }

        //    return false;
        //}
        private void UpdateOverlayVisibility()
        {
            if (_isDisposed || _targetWindowHandle == IntPtr.Zero)
                return;

            bool shouldBeVisible = IsWindowActive(_targetWindowHandle);
            
            try
            {
                if (_mainWindow.IsDisposed || !_mainWindow.IsHandleCreated)
                    return;

                _mainWindow.Invoke(new Action(() =>
                {
                    if (_isDisposed || _mainWindow.IsDisposed || !((MainWindow)_mainWindow).IsOverlayRunning())
                        return;

                    if (_overlayForm.EditMode)
                        return;

                    if (_overlayForm.Visible != shouldBeVisible)
                    {
                        //Debug.WriteLine($"Visibility change: {shouldBeVisible}, Handle: {_targetWindowHandle}, Active: {IsWindowActive(_targetWindowHandle)}");
                        _overlayForm.Visible = shouldBeVisible;

                        // Принудительно обновляем окно
                        if (shouldBeVisible)
                        {
                            _overlayForm.BringToFront();
                            _overlayForm.Refresh();
                        }
                    }
                }));
            }
            catch (InvalidOperationException)
            {
                // Handle уже уничтожен - это нормально при закрытии
            }
        }
        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_isDisposed) 
                    return;

                _isDisposed = true;

                if (_hookHandle != IntPtr.Zero)
                {
                    UnhookWinEvent(_hookHandle);
                    _hookHandle = IntPtr.Zero;
                }

                _targetWindowHandle = IntPtr.Zero;
            }
        }
        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax,
            IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
            uint idProcess, uint idThread, uint dwFlags);
        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    }
}
