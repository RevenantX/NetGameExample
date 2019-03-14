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

public class ClientLogic : MonoBehaviour, INetEventListener
{
    private NetManager _netManager;
    private NetDataWriter _writer;
    private NetPacketProcessor _packetProcessor;
    private Dictionary<long, BasePlayer> _players;
    private ClientPlayer _clientPlayer;
    private LogicTimer _logicTimer;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _logicTimer = new LogicTimer(OnLogicUpdate);
        _players = new Dictionary<long, BasePlayer>();
        _writer = new NetDataWriter();
        _packetProcessor = new NetPacketProcessor();
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
        if(_clientPlayer != null)
            _clientPlayer.Update(LogicTimer.FixedDelta);
    }

    private void Update()
    {
        _netManager.PollEvents();
        _logicTimer.Update();
    }

    private void OnDestroy()
    {
        _netManager.Stop();
    }

    private void OnPlayerJoined(PlayerJoinedPacket packet)
    {
        _players.Add(packet.Id, new RemotePlayer());
    }

    private void OnJoinAccept(JoinAcceptPacket packet)
    {
        Debug.Log("[C] Join accept. Received player id: " + packet.Id);
        _clientPlayer = new ClientPlayer(packet.Id);
    }

    private void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
    {
        _writer.Reset();
        _writer.Put((byte)PacketType.Serialized);
        _packetProcessor.Write(_writer, packet);
        _netManager.FirstPeer.Send(_writer, deliveryMethod);
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        Debug.Log("[C] Connected to server: " + peer.EndPoint);
        Random r = new Random();
        SendPacket(new JoinPacket{ UserName = Environment.MachineName + " " + r.Next(100000) }, DeliveryMethod.ReliableOrdered);
        _logicTimer.Start();
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logicTimer.Stop();
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
            case PacketType.Serialized:
                _packetProcessor.ReadAllPackets(reader);
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
        request.Reject();
    }

    public void Connect(string ip)
    {
        _netManager.Connect(ip, 9000, "ExampleGame");
    }
}
