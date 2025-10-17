using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCR_DPS_Monitor
{
    public class PlayerData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int PartyNumber { get; set; } // 1 или 2
        public int ClassType { get; set; } // 0 - саппорт, 1 - ДД
        public double Value { get; set; } // текущее значение урона/хила
        public bool IsConnected { get; set; } = true;
        public double Percentage { get; set; } // расчетный процент
    }

    public class PartyManager
    {
        private List<PlayerData> _players = new List<PlayerData>();
        private OverlayBlocksForm _overlayForm;

        public PartyManager(OverlayBlocksForm overlayForm)
        {
            _overlayForm = overlayForm;
        }

        public void AddPlayer(PlayerData player)
        {
            // Проверяем, есть ли игрок с таким же именем в отключенных (альтернативная проверка)
            var disconnectedPlayerWithSameName = _players.FirstOrDefault(p =>
                p.Name == player.Name && !p.IsConnected && p.PartyNumber == player.PartyNumber);

            if (disconnectedPlayerWithSameName != null)
            {
                // Удаляем старого отключенного игрока с таким же именем
                _players.RemoveAll(p => p.Id == disconnectedPlayerWithSameName.Id);
            }

            // Проверяем, не превышен ли лимит в группе (макс 4 игрока)
            var partyPlayers = _players.Where(p => p.PartyNumber == player.PartyNumber).ToList();

            if (partyPlayers.Count >= 4)
            {
                // Ищем отключенного игрока в этой группе
                var disconnectedPlayer = partyPlayers.FirstOrDefault(p => !p.IsConnected);
                if (disconnectedPlayer != null)
                {
                    //Удаляем отключенного игрока
                    _players.RemoveAll(p => p.Id == disconnectedPlayer.Id);
                }
                else
                {
                    // Нет свободных мест - не добавляем игрока
                    return;
                }
            }

            // Добавляем/обновляем игрока
            var existingPlayer = _players.FirstOrDefault(p => p.Id == player.Id);
            if (existingPlayer != null)
            {
                existingPlayer.Name = player.Name;
                existingPlayer.PartyNumber = player.PartyNumber;
                existingPlayer.ClassType = player.ClassType;
                existingPlayer.IsConnected = true;
            }
            else
            {
                _players.Add(player);
            }

            UpdateOverlay();
        }

        public void Disconnect()
        {
            // Проставляем всем игрокам isConnected = false
            //foreach (var player in _players)
            //{
            //    player.IsConnected = false;
            //}
            _players.Clear();
            UpdateOverlay();
        }

        public void RemovePlayer(string playerId)
        {
            var player = _players.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
            {
                player.IsConnected = false;
                UpdateOverlay();
            }
        }

        public void UpdatePlayerData(string playerId, double value)
        {
            var player = _players.FirstOrDefault(p => p.Id == playerId);
            if (player != null && player.IsConnected)
            {
                player.Value = value;
                CalculatePercentages();
                UpdateOverlay();
            }
        }

        private void CalculatePercentages()
        {
            // Сбрасываем проценты
            foreach (var player in _players)
            {
                player.Percentage = 0;
            }

            // Находим общую сумму урона всех ДД (из обеих групп)
            double totalDDDamageAllParties = _players
                .Where(p => p.ClassType == 1) // Все ДД
                .Sum(p => p.Value);

            // Группируем по группам
            var parties = _players.GroupBy(p => p.PartyNumber);

            foreach (var party in parties)
            {
                var ddPlayers = party.Where(p => p.ClassType == 1).ToList();
                var supportPlayers = party.Where(p => p.ClassType == 0).ToList();

                // Сумма ДД в текущей группе (для расчета саппортов)
                double totalDDDamageInParty = ddPlayers.Sum(p => p.Value);

                // Расчет процентов для ДД (от общей суммы всех ДД)
                if (totalDDDamageAllParties > 0)
                {
                    foreach (var ddPlayer in ddPlayers)
                    {
                        ddPlayer.Percentage = ddPlayer.Value / totalDDDamageAllParties * 100;
                    }
                }

                // Расчет процентов для саппортов (от суммы ДД в их группе)
                foreach (var support in supportPlayers)
                {
                    double effectiveDamage = Math.Max(0, totalDDDamageInParty - support.Value);
                    if (effectiveDamage > 0)
                    {
                        support.Percentage = support.Value / effectiveDamage * 100;
                    }
                }
            }
        }

        private void UpdateOverlay()
        {
            CalculatePercentages();

            // УБИРАЕМ фильтрацию по IsConnected - берем всех игроков группы
            var party1Players = _players
                .Where(p => p.PartyNumber == 1) // убираем && p.IsConnected
                .OrderBy(p => p.ClassType == 1 ? 0 : 1) // ДД сначала
                .ThenBy(p => p.ClassType == 1 ? 0 : 1)
                .Take(4)
                .ToList();

            var party2Players = _players
                .Where(p => p.PartyNumber == 2) // убираем && p.IsConnected
                .OrderBy(p => p.ClassType == 1 ? 0 : 1) // ДД сначала
                .ThenBy(p => p.ClassType == 1 ? 0 : 1)
                .Take(4)
                .ToList();
            
            // Обновляем первую группу
            for (int i = 0; i < 4; i++)
            {
                if (i < party1Players.Count)
                {
                    var player = party1Players[i];
                    _overlayForm.UpdatePlayerData(0, i, player.Name, player.Value, player.Percentage, player.IsConnected, player.ClassType);
                }
                else
                {
                    _overlayForm.UpdatePlayerData(0, i, "", 0, 0, false, 5);
                }
            }

            // Обновляем вторую группу
            for (int i = 0; i < 4; i++)
            {
                if (i < party2Players.Count)
                {
                    var player = party2Players[i];
                    _overlayForm.UpdatePlayerData(1, i, player.Name, player.Value, player.Percentage, player.IsConnected, player.ClassType);
                }
                else
                {
                    _overlayForm.UpdatePlayerData(1, i, "", 0, 0, false, 5);
                }
            }
        }

        public List<PlayerData> GetPlayers() => _players.ToList();
    }
}
