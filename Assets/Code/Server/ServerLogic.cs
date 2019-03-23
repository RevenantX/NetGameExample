using System.Net;
using System.Net.Sockets;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Code.Server
{
    public class ServerLogic : MonoBehaviour, INetEventListener
    {
        private NetManager _netManager;
        private NetPacketProcessor _packetProcessor;
        private ServerPlayer[] _players;
        private int _playersCount;
        private const int MaxPlayers = 8;
        private LogicTimer _logicTimer;
        private readonly NetDataWriter _cachedWriter = new NetDataWriter();
        private AntilagSystem _antilagSystem;
        private ushort _serverTick;

        private PlayerInputPacket _cachedCommand = new PlayerInputPacket();
        private ServerState _serverState = new ServerState();

        public void StartServer()
        {
            if (_netManager.IsRunning)
                return;
            _netManager.Start(9000);
            _logicTimer.Start();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _antilagSystem = new AntilagSystem(60, MaxPlayers);
            _logicTimer = new LogicTimer(OnLogicUpdate);
            _players = new ServerPlayer[MaxPlayers];
            _packetProcessor = new NetPacketProcessor();
            
            //register auto serializable vector2
            _packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector2());
           
            //register auto serializable PlayerState
            _packetProcessor.RegisterNestedType<PlayerState>();
            
            _packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);
            _netManager = new NetManager(this)
            {
                AutoRecycle = true
            };
        }

        private void OnDestroy()
        {
            _netManager.Stop();
            _logicTimer.Stop();
        }

        private void OnLogicUpdate()
        {
            _serverTick = (ushort)((_serverTick + 1) % NetworkGeneral.MaxGameSequence);

            if (_serverState.PlayerStates == null || _serverState.PlayerStates.Length < _playersCount)
                _serverState.PlayerStates = new PlayerState[_playersCount];
            
            for (int i = 0; i < _playersCount; i++)
            {
                var p = _players[i];
                p.Update(LogicTimer.FixedDelta);
                _serverState.PlayerStates[i] = p.NetworkState;
            }

            if (_serverTick % 2 == 0)
                SendServerState();
        }

        private void SendServerState()
        {         
            _serverState.Tick = _serverTick;
            
            for (int i = 0; i < _playersCount; i++)
            {
                var p = _players[i]; 
                int statesMax = p.AssociatedPeer.GetMaxSinglePacketSize(DeliveryMethod.Unreliable) - ServerState.HeaderSize;
                statesMax /= PlayerState.Size;
                
                for (int s = 0; s < (_playersCount-1)/statesMax + 1; s++)
                {
                    //TODO: change
                    _serverState.PlayerStatesCount = _playersCount;
                    _serverState.StartState = s * statesMax;
                    p.AssociatedPeer.Send(WriteSerializable(PacketType.ServerState, _serverState), DeliveryMethod.Unreliable);
                }
            }
        }

        private void Update()
        {
            _netManager.PollEvents();
            _logicTimer.Update();
        }
        
        private NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable
        {
            _cachedWriter.Reset();
            _cachedWriter.Put((byte) type);
            packet.Serialize(_cachedWriter);
            return _cachedWriter;
        }

        private NetDataWriter WritePacket<T>(T packet) where T : class, new()
        {
            _cachedWriter.Reset();
            _cachedWriter.Put((byte) PacketType.Serialized);
            _packetProcessor.Write(_cachedWriter, packet);
            return _cachedWriter;
        }

        private void OnJoinReceived(JoinPacket joinPacket, NetPeer peer)
        {
            Debug.Log("[S] Join packet received: " + joinPacket.UserName);
            var player = new ServerPlayer(joinPacket.UserName, peer);
            _players[_playersCount] = player;
            _playersCount++;

            player.Spawn(new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f)));

            //Send join accept
            var ja = new JoinAcceptPacket {Id = peer.Id};
            peer.Send(WritePacket(ja), DeliveryMethod.ReliableOrdered);

            //Send to old players info about new player
            var pj = new PlayerJoinedPacket();
            pj.UserName = joinPacket.UserName;
            pj.NewPlayer = true;
            pj.InitialPlayerState = player.NetworkState;
            _netManager.SendToAll(WritePacket(pj), DeliveryMethod.ReliableOrdered, peer);

            //Send to new player info about old players
            pj.NewPlayer = false;
            for (int i = 0; i < _playersCount - 1; i++)
            {
                var otherPlayer = _players[i];
                pj.UserName = otherPlayer.Name;
                pj.InitialPlayerState = otherPlayer.NetworkState;
                peer.Send(WritePacket(pj), DeliveryMethod.ReliableOrdered);
            }
        }

        private void OnInputReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null)
                return;
            _cachedCommand.Deserialize(reader);
            var player = (ServerPlayer) peer.Tag;
            
            bool antilagApplied = _antilagSystem.TryApplyAntilag(_players, _serverTick, peer.Id);
            player.ApplyInput(_cachedCommand, LogicTimer.FixedDelta);
            if(antilagApplied)
                _antilagSystem.RevertAntilag(_players);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[S] Player connected: " + peer.EndPoint);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log("[S] Player disconnected: " + disconnectInfo.Reason);

            if (peer.Tag != null)
            {
                var plp = new PlayerLeavedPacket {Id = (byte)peer.Id};
                _netManager.SendToAll(WritePacket(plp), DeliveryMethod.ReliableOrdered);
            }
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log("[S] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;
            PacketType pt = (PacketType) packetType;
            switch (pt)
            {
                case PacketType.Movement:
                    OnInputReceived(reader, peer);
                    break;
                case PacketType.Serialized:
                    _packetProcessor.ReadAllPackets(reader, peer);
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
            request.AcceptIfKey("ExampleGame");
        }
    }
}