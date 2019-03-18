using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Code.Client;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using Random = System.Random;

namespace Code.Client
{
    public class ClientLogic : MonoBehaviour, INetEventListener
    {
        [SerializeField] private ClientPlayerView _clientPlayerViewPrefab;
        [SerializeField] private RemotePlayerView _remotePlayerViewPrefab;

        private NetManager _netManager;
        private NetDataWriter _writer;
        private NetPacketProcessor _packetProcessor;
        private Dictionary<long, BasePlayer> _players;
        private ClientPlayer _clientPlayer;
        private string _userName;
        private ServerState _cachedServerState;

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
            _netManager = new NetManager(this)
            {
                AutoRecycle = true
            };
            _netManager.Start();
        }

        private void OnLogicUpdate()
        {
            foreach (var kv in _players)
            {
                kv.Value.Update(LogicTimer.FixedDelta);
            }
        }

        private void Update()
        {
            _netManager.PollEvents();
            LogicTimer.Update();
        }

        private void OnDestroy()
        {
            _netManager.Stop();
        }

        private void OnPlayerJoined(PlayerJoinedPacket packet)
        {
            _players.Add(packet.InitialPlayerState.Id, new RemotePlayer(packet.UserName, packet.InitialPlayerState));
        }

        private void OnServerState()
        {
            for (int i = 0; i < _cachedServerState.PlayerStates.Length; i++)
            {
                //servstate
            }
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
            _writer.Reset();
            _writer.Put((byte)type);
            packet.Serialize(_writer);
            _netManager.FirstPeer.Send(_writer, deliveryMethod);
        }

        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
        {
            _writer.Reset();
            _writer.Put((byte) PacketType.Serialized);
            _packetProcessor.Write(_writer, packet);
            _netManager.FirstPeer.Send(_writer, deliveryMethod);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[C] Connected to server: " + peer.EndPoint);
            SendPacket(new JoinPacket {UserName = _userName}, DeliveryMethod.ReliableOrdered);
            LogicTimer.Start();
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
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