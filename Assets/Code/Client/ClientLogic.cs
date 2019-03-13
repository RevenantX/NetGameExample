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
    private Dictionary<long, ClientPlayer> _players;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _players = new Dictionary<long, ClientPlayer>();
        _writer = new NetDataWriter();
        _packetProcessor = new NetPacketProcessor();
        _netManager = new NetManager(this)
        {
            AutoRecycle = true
        };
        _netManager.Start();
    }

    private void Update()
    {
        _netManager.PollEvents();
    }

    private void OnDestroy()
    {
        _netManager.Stop();
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
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
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
