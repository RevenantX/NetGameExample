using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Code.Client;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace Code.Client
{
    public class ClientLogic : MonoBehaviour, INetEventListener
    {
        [SerializeField] private ClientPlayerView _clientPlayerViewPrefab;
        [SerializeField] private RemotePlayerView _remotePlayerViewPrefab;
        [SerializeField] private Text _debugText;
        [SerializeField] private ShootEffect _shootEffectPrefab;
        
        private GamePool<ShootEffect> _shootsPool;
        
        private NetManager _netManager;
        private NetDataWriter _writer;
        private NetPacketProcessor _packetProcessor;


        private string _userName;
        private ServerState _cachedServerState;
        private ShootPacket _cachedShootData;
        private ushort _lastServerTick;
        private NetPeer _server;
        private ClientPlayerManager _playerManager;
        private int _ping;

        public static LogicTimer LogicTimer { get; private set; }

        private ShootEffect ShootEffectContructor()
        {
            var eff = Instantiate(_shootEffectPrefab);
            eff.Init(e => _shootsPool.Put(e));
            return eff;
        }
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Random r = new Random();
            _cachedServerState = new ServerState();
            _cachedShootData = new ShootPacket();
            _userName = Environment.MachineName + " " + r.Next(100000);
            LogicTimer = new LogicTimer(OnLogicUpdate);
            _writer = new NetDataWriter();
            _playerManager = new ClientPlayerManager(this);
            _shootsPool = new GamePool<ShootEffect>(ShootEffectContructor, 100);
            _packetProcessor = new NetPacketProcessor();
            _packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector2());
            _packetProcessor.RegisterNestedType<PlayerState>();
            _packetProcessor.SubscribeReusable<PlayerJoinedPacket>(OnPlayerJoined);
            _packetProcessor.SubscribeReusable<JoinAcceptPacket>(OnJoinAccept);
            _packetProcessor.SubscribeReusable<PlayerLeavedPacket>(OnPlayerLeaved);
            _netManager = new NetManager(this)
            {
                AutoRecycle = true
            };
            _netManager.Start();
        }

        private void OnLogicUpdate()
        {
            _playerManager.LogicUpdate();
        }

        private void Update()
        {
            _netManager.PollEvents();
            LogicTimer.Update();
            if (_playerManager.OurPlayer != null)
                _debugText.text =
                    string.Format(
                        $"LastServerTick: {_lastServerTick}\n" + 
                        $"StoredCommands: {_playerManager.OurPlayer.StoredCommands}\n" + 
                        $"Ping: {_ping}");
            else
                _debugText.text = "Disconnected";
        }

        private void OnDestroy()
        {
            _netManager.Stop();
        }

        private void OnPlayerJoined(PlayerJoinedPacket packet)
        {
            Debug.Log($"[C] Player joined: {packet.UserName}");
            var remotePlayer = new RemotePlayer(_playerManager, packet.UserName, packet);
            var view = RemotePlayerView.Create(_remotePlayerViewPrefab, remotePlayer);
            _playerManager.AddPlayer(remotePlayer, view);
        }

        private void OnServerState()
        {
            //skip duplicate or old because we received that packet unreliably
            if (NetworkGeneral.SeqDiff(_cachedServerState.Tick, _lastServerTick) <= 0)
                return;
            _lastServerTick = _cachedServerState.Tick;
            _playerManager.ApplyServerState(ref _cachedServerState);
        }

        private void OnShoot()
        {
            var p = _playerManager.GetById(_cachedShootData.FromPlayer);
            if (p == null || p == _playerManager.OurPlayer)
                return;
            SpawnShoot(p.Position, _cachedShootData.Hit);
        }

        public void SpawnShoot(Vector2 from, Vector2 to)
        {
            var eff = _shootsPool.Get();
            eff.Spawn(from, to);
        }

        private void OnPlayerLeaved(PlayerLeavedPacket packet)
        {
            var player = _playerManager.RemovePlayer(packet.Id);
            if(player != null)
                Debug.Log($"[C] Player leaved: {player.Name}");
        }

        private void OnJoinAccept(JoinAcceptPacket packet)
        {
            Debug.Log("[C] Join accept. Received player id: " + packet.Id);
            var clientPlayer = new ClientPlayer(this, _playerManager, _userName, packet.Id);
            var view = ClientPlayerView.Create(_clientPlayerViewPrefab, clientPlayer);
            _playerManager.AddClientPlayer(clientPlayer, view);
        }

        public void SendPacketSerializable<T>(PacketType type, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)type);
            packet.Serialize(_writer);
            _server.Send(_writer, deliveryMethod);
        }

        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte) PacketType.Serialized);
            _packetProcessor.Write(_writer, packet);
            _server.Send(_writer, deliveryMethod);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[C] Connected to server: " + peer.EndPoint);
            _server = peer;
            
            SendPacket(new JoinPacket {UserName = _userName}, DeliveryMethod.ReliableOrdered);
            LogicTimer.Start();
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _server = null;
            LogicTimer.Stop();
            Debug.Log("[C] Disconnected from server: " + disconnectInfo.Reason);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log("[C] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;
            PacketType pt = (PacketType) packetType;
            switch (pt)
            {
                case PacketType.Spawn:
                    break;
                case PacketType.ServerState:
                    _cachedServerState.Deserialize(reader);
                    OnServerState();
                    break;
                case PacketType.Serialized:
                    _packetProcessor.ReadAllPackets(reader);
                    break;
                case PacketType.Shoot:
                    _cachedShootData.Deserialize(reader);
                    OnShoot();
                    break;
                default:
                    Debug.Log("Unhandled packet: " + pt);
                    break;
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
            UnconnectedMessageType messageType)
        {

        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            _ping = latency;
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }

        public void Connect(string ip)
        {
            _netManager.Connect(ip, 10515, "ExampleGame");
        }
    }
}