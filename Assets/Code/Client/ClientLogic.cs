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

        private NetManager _netManager;
        private NetDataWriter _writer;
        private NetPacketProcessor _packetProcessor;
        private Dictionary<long, BasePlayer> _players;
        private ClientPlayer _clientPlayer;
        private string _userName;
        private ServerState _cachedServerState;
        private ushort _lastServerTick;
        private NetPeer _server;

        public static LogicTimer LogicTimer { get; private set; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Random r = new Random();
            _cachedServerState = new ServerState();
            _userName = Environment.MachineName + " " + r.Next(100000);
            LogicTimer = new LogicTimer(OnLogicUpdate);
            _players = new Dictionary<long, BasePlayer>();
            _writer = new NetDataWriter();
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
            foreach (var kv in _players)
                kv.Value.Update(LogicTimer.FixedDelta);
        }

        private void Update()
        {
            _netManager.PollEvents();
            LogicTimer.Update();
            if(_clientPlayer != null)
                _debugText.text = string.Format($"LastServerTick: {_lastServerTick}\nStoredCommands: {_clientPlayer.StoredCommands}");
        }

        private void OnDestroy()
        {
            _netManager.Stop();
        }

        private void OnPlayerJoined(PlayerJoinedPacket packet)
        {
            Debug.Log($"[C] Player joined: {packet.UserName}");
            var remotePlayer = new RemotePlayer(packet.UserName, packet.InitialPlayerState);
            RemotePlayerView.Create(_remotePlayerViewPrefab, remotePlayer);
            _players.Add(packet.InitialPlayerState.Id, remotePlayer);
        }

        private void OnServerState()
        {
            //skip duplicate or old because we received that packet unreliably
            if (NetworkGeneral.SeqDiff(_cachedServerState.Tick, _lastServerTick) <= 0)
                return;
            _lastServerTick = _cachedServerState.Tick;
            
            for (int i = 0; i < _cachedServerState.PlayerStatesCount; i++)
            {
                var state = _cachedServerState.PlayerStates[i];
                if(!_players.TryGetValue(state.Id, out var player))
                    continue;

                if (player == _clientPlayer)
                {
                    _clientPlayer.ReceiveServerState(_cachedServerState, state);
                }
                else
                {
                    var rp = (RemotePlayer)player;
                    rp.OnPlayerState(state);
                }
            }
        }

        private void OnPlayerLeaved(PlayerLeavedPacket packet)
        {
            if(!_players.TryGetValue(packet.Id, out var player))
                return;
            Debug.Log($"[C] Player leaved: {player.Name}");
            _players.Remove(packet.Id);
        }

        private void OnJoinAccept(JoinAcceptPacket packet)
        {
            Debug.Log("[C] Join accept. Received player id: " + packet.Id);
            _clientPlayer = new ClientPlayer(this, _userName, packet.Id);
            ClientPlayerView.Create(_clientPlayerViewPrefab, _clientPlayer);
            _players.Add(packet.Id, _clientPlayer);
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

        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }

        public void Connect(string ip)
        {
            _netManager.Connect(ip, 9000, "ExampleGame");
        }
    }
}