using SocketIO;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using System.Diagnostics;


namespace OCR_DPS_Monitor
{
    public class LobbyManager
    {
        private SocketIOClient.SocketIO _client;
        private string _playerId;
        private string _lobbyCode;

        private bool _intentionalDisconnect = false;
        private DateTime _lastDisconnectTime;
        private const double RECONNECT_THRESHOLD_SECONDS = 5;

        // Хранилище информации об игроках
        public Dictionary<string, PlayerInfo> Players { get; private set; } = new Dictionary<string, PlayerInfo>();
        public string CurrentPlayerId { get; set; }
        public string CurrentPlayerName { get; set; }

        public event Action<string> OnLobbyCreated;
        public event Action<string> OnJoinError;
        public event Action<PlayerInfo> OnPlayerJoined;
        public event Action<string, string> OnPlayerLeft; 
        public event Action<PlayerDataUpdate> OnPlayerDataReceived;
        public event Action<PlayerInfo[]> OnAllPlayersData;
        public event Action<int> OnPlayerDisconnected;

        public class ClassTypes
        {
            public const int Support = 0;
            public const int DD = 1;
        }

        public string GetCurrentPlayerId()
        {
            // Ищем себя в словаре игроков по имени
            var currentPlayer = Players.Values.FirstOrDefault(player =>
                player.name.Equals(CurrentPlayerName, StringComparison.OrdinalIgnoreCase));

            return currentPlayer?.id;
        }

        public async Task ConnectToServer(string serverUrl)
        {
            _client = new SocketIOClient.SocketIO(serverUrl);

            // Обработчики событий от сервера
            _client.On("allPlayersData", (response) =>
            {
                var json = response.ToString();

                var players = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerInfo[]>(json);
                HandleAllPlayersData(players);
                CurrentPlayerId = GetCurrentPlayerId();
            });

            _client.On("playerJoined", (response) =>
            {
                var json = response.ToString();

                var dataArray = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerJoinedResponse[]>(json);
                if (dataArray != null && dataArray.Length > 0)
                {
                    var data = dataArray[0]; // Берем первый элемент
                    HandlePlayerJoined(data.player);
                }
                //var data = response.GetValue<PlayerJoinedResponse>();
                //HandlePlayerJoined(data.player);
            });

            _client.On("playerLeft", (response) =>
            {
                var json = response.ToString();

                var dataArray = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerLeftResponse[]>(json);
                if (dataArray != null && dataArray.Length > 0)
                {
                    var data = dataArray[0]; // Берем первый элемент
                    HandlePlayerLeft(data.playerId);
                }
                else
                {
                    Debug.WriteLine("playerLeft: пустой массив данных");
                }
                //var data = response.GetValue<PlayerLeftResponse>();
                //HandlePlayerLeft(data.playerId);
            });

            _client.On("playerDataUpdate", (response) =>
            {
            var json = response.ToString();

            var dataArray = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerDataUpdate[]>(json);
                if (dataArray != null && dataArray.Length > 0)
                {
                    var data = dataArray[0]; // Берем первый элемент
                    OnPlayerDataReceived?.Invoke(data);
                }

                //var data = response.GetValue<PlayerDataUpdate>();
                //OnPlayerDataReceived?.Invoke(data);
            });

            _client.OnConnected += (sender, e) =>
            {
                _intentionalDisconnect = false;
                Debug.WriteLine("Connected to server");
            };

            _client.OnDisconnected += (sender, e) =>
            {
                var now = DateTime.Now;

                // Если отключение было намеренным или прошло мало времени с последнего отключения - считаем временным
                if (_intentionalDisconnect || (now - _lastDisconnectTime).TotalSeconds < RECONNECT_THRESHOLD_SECONDS)
                {
                    Debug.WriteLine("Временное отключение, ожидаем переподключения...");
                    // Не вызываем OnPlayerDisconnected для временных отключений
                }
                else
                {
                    Debug.WriteLine("Окончательное отключение от сервера");
                    OnPlayerDisconnected?.Invoke(0);
                }

                _lastDisconnectTime = now;
                _intentionalDisconnect = false;
                //OnPlayerDisconnected?.Invoke(0);
                //Debug.WriteLine("Disconnected from server");
            };

            _client.OnError += (sender, error) =>
            {
                Debug.WriteLine($"Connection error: {error}");
                //throw new Exception($"Failed to connect: {error}");
            };

            // Пытаемся подключиться с таймаутом
            var connectTask = _client.ConnectAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(RECONNECT_THRESHOLD_SECONDS));

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                await _client.DisconnectAsync(); // Отключаемся
                throw new Exception("Connection timeout - server is not available");
            }

            // Если подключение успешно, продолжаем нормально
            await connectTask;
        }

        public void IntentionalDisconnect()
        {
            _intentionalDisconnect = true;
            _client.DisconnectAsync();
        }

        // Обработка получения данных о всех игроках при подключении
        private void HandleAllPlayersData(PlayerInfo[] players)
        {
            Players.Clear();

            if (players == null)
            {
                Debug.WriteLine("Получен пустой список игроков");
                return;
            }

            foreach (var player in players)
            {
                Players[player.id] = player;
                Debug.WriteLine($"Получен игрок: {player.name} (ID: {player.id}), Группа: {player.partyNumber}, Класс: {player.classType}");
            }
            OnAllPlayersData?.Invoke(players);
        }

        // Обработка подключения нового игрока
        private void HandlePlayerJoined(PlayerInfo player)
        {
            Players[player.id] = player;
            OnPlayerJoined?.Invoke(player);
            Debug.WriteLine($"Новый игрок присоединился: {player.name} (ID: {player.id})");
        }

        // Обработка отключения игрока
        private void HandlePlayerLeft(string playerId)
        {
            if (Players.ContainsKey(playerId))
            {
                var playerName = Players[playerId].name;
                Players.Remove(playerId);
                OnPlayerLeft?.Invoke(playerId, playerName);
                Debug.WriteLine($"Игрок покинул лобби: {playerName} (ID: {playerId})");
            }
        }

        public async Task CreateLobby(string playerName, int partyNumber, int classType)
        {
            if (_client == null || !_client.Connected) return;

            var playerData = new
            {
                name = playerName,
                partyNumber = partyNumber,
                classType = classType
            };

            await _client.EmitAsync("createLobby", (response) =>
            {
                try
                {
                    var json = response.ToString();

                    // Десериализуем массив и берем первый элемент
                    var dataArray = Newtonsoft.Json.JsonConvert.DeserializeObject<LobbyCreationResponse[]>(json);

                    if (dataArray != null && dataArray.Length > 0)
                    {
                        var data = dataArray[0];

                        if (data.success)
                        {
                            _playerId = data.playerId;
                            _lobbyCode = data.lobbyCode;
                            OnLobbyCreated?.Invoke(_lobbyCode);
                            CurrentPlayerName = playerName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка: {ex.Message}");
                }
            }, playerData);
        }

        public async Task JoinLobby(string code, string playerName, int partyNumber, int classType)
        {
            if (_client == null || !_client.Connected) return;

            var joinData = new
            {
                lobbyCode = code,
                playerData = new
                {
                    name = playerName,
                    partyNumber = partyNumber,
                    classType = classType
                }
            };

            var completionSource = new TaskCompletionSource<bool>();

            await _client.EmitAsync("joinLobby", (response) =>
            {
                try
                {
                    var jsonString = response.ToString();

                    // Десериализуем JSON
                    var dataArray = Newtonsoft.Json.JsonConvert.DeserializeObject<JoinLobbyResponse[]>(jsonString);
                    if (dataArray != null && dataArray.Length > 0)
                    {
                        var data = dataArray[0];
                        //var data = response.GetValue<JoinLobbyResponse>();
                        if (data.success)
                        {
                            _playerId = data.playerId;
                            _lobbyCode = code;
                            CurrentPlayerName = playerName;
                            completionSource.SetResult(true);
                        }
                        else
                        {
                            //OnJoinError?.Invoke(data.error);
                            completionSource.SetException(new Exception(data.error));
                        }
                    }
                    else
                    {
                        //OnJoinError?.Invoke("Empty response from server");
                        completionSource.SetException(new Exception("Empty response from server"));
                    }
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
            }, joinData);

            await completionSource.Task;
        }

        public async Task SendPlayerData(double value)
        {
            if (_client == null || !_client.Connected || string.IsNullOrEmpty(_lobbyCode))
            {
                Debug.WriteLine("Не подключен к серверу или лобби");
                return;
            }

            try
            {
                var data = new
                {
                    value = value,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                
                await _client.EmitAsync("playerData", data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отправки данных: {ex.Message}");
            }
        }

        public async Task Disconnect()
        {
            Players.Clear();
            CurrentPlayerId = "";
            if (_client != null)
            {
                await _client.DisconnectAsync();
                _client.Dispose();
            }
        }
    }

    // Классы данных
    public class LobbyCreationResponse
    {
        public bool success;
        public string lobbyCode;
        public string playerId;
    }

    public class JoinLobbyResponse
    {
        public bool success;
        public string error;
        public string playerId;
    }

    public class AllPlayersDataResponse
    {
        public PlayerInfo[] players;
    }

    public class PlayerJoinedResponse
    {
        public PlayerInfo player;
    }

    public class PlayerLeftResponse
    {
        public string playerId;
    }

    public class PlayerDataUpdate
    {
        public string playerId;
        public double value;
        public string timestamp;
    }

    public class PlayerInfo
    {
        public string id;
        public string name;
        public int partyNumber;
        public int classType; // 0 = Support, 1 = DD
        public bool isHost;
    }
}
