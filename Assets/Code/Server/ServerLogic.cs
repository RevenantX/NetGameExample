using System.Net;
using System.Net.Sockets;
using Code.Server;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class ServerLogic : MonoBehaviour, INetEventListener
{
    private NetManager _netManager;
    private NetPacketProcessor _packetProcessor;
    private ServerPlayer[] _players;
    private int _playersCount;
    private const int MaxPlayers = 8;
    private LogicTimer _logicTimer;
    private readonly NetDataWriter _cachedWriter = new NetDataWriter();

    private MovementPacket _cachedCommand = new MovementPacket();
    
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
        _logicTimer = new LogicTimer(OnLogicUpdate);
        _players = new ServerPlayer[MaxPlayers];
        _packetProcessor = new NetPacketProcessor();
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
        for (int i = 0; i < _playersCount; i++)
        {
            _players[i].Update(LogicTimer.FixedDelta);
        }
    }

    private void Update()
    {
        _netManager.PollEvents();
        _logicTimer.Update();
    }

    private NetDataWriter WritePacket<T>(T packet) where T : class, new()
    {
        _cachedWriter.Reset();
        _cachedWriter.Put((byte)PacketType.Serialized);
        _packetProcessor.Write(_cachedWriter, packet);
        return _cachedWriter;
    }

    private void OnJoinReceived(JoinPacket joinPacket, NetPeer peer)
    {
        Debug.Log("[S] Join packet received: " + joinPacket.UserName);
        var player = new ServerPlayer(peer);
        _players[_playersCount] = player;
        _playersCount++;
        
        player.Spawn(new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f)));

        //Send join accept
        var ja = new JoinAcceptPacket {Id = peer.Id};
        peer.Send(WritePacket(ja), DeliveryMethod.ReliableOrdered);
        
        //Send to old players info about new player
        var pj = new PlayerJoinedPacket();
        pj.UserName = joinPacket.UserName;
        pj.Id = peer.Id;
        pj.NewPlayer = true;
        _netManager.SendToAll(WritePacket(pj), DeliveryMethod.ReliableOrdered, peer);

        //Send to new player info about old players
        pj.NewPlayer = false;
        for (int i = 0; i < _playersCount - 1; i++)
        {
            pj.Id = _players[i].AssociatedPeer.Id;
            pj.UserName = _players[i].Name;
            peer.Send(WritePacket(pj), DeliveryMethod.ReliableOrdered);
        }
    }

    private void OnMovementReceived(NetPacketReader reader, NetPeer peer)
    {
        if (peer.Tag == null)
            return;
        _cachedCommand.Deserialize(reader);
        var player = (ServerPlayer) peer.Tag;
        player.Move(_cachedCommand, LogicTimer.FixedDelta);
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        Debug.Log("[S] Player connected: " + peer.EndPoint);
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("[S] Player disconnected: " + disconnectInfo.Reason);
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
                OnMovementReceived(reader, peer);
                break;
            case PacketType.Serialized:
                _packetProcessor.ReadAllPackets(reader, peer);
                break;
            default:
                Debug.Log("Unhandled packet: " + pt);
                break;
        }
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
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
