using System.Diagnostics;
using System.Runtime.InteropServices;

public class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int VK_CONTROL = 0x11;
    private const int VK_SHIFT = 0x10;
    private const int VK_OEM_PLUS = 0xBB;    // Обычный +
    private const int VK_ADD = 0x6B;         // NumPad +
    private const int VK_O = 0x4F;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;

    public event EventHandler<Keys> KeyPressed;

    public KeyboardHook()
    {
        _proc = HookCallback;
        _hookID = SetHook(_proc);
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            // Правильная проверка модификаторов через GetAsyncKeyState
            bool controlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

            // Проверяем, что нажаты Ctrl+Shift+Plus (оба варианта +)
            if (controlPressed && shiftPressed && (vkCode == VK_OEM_PLUS || vkCode == VK_ADD))
            {
                KeyPressed?.Invoke(this, (Keys)vkCode);
                return (IntPtr)1; // Блокируем дальнейшую обработку
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(_hookID);
    }
}