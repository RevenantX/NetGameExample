using System;
using Code.Server;
using LiteNetLib.Utils;
using UnityEngine;

namespace Code.Shared
{
    public enum PacketType : byte
    {
        Movement,
        Spawn,
        ServerState,
        Serialized
    }
    
    //Auto serializable packets
    public class JoinPacket
    {
        public string UserName { get; set; }
    }

    public class JoinAcceptPacket
    {
        public long Id { get; set; }
    }

    public class PlayerJoinedPacket
    {
        public string UserName { get; set; }
        public bool NewPlayer { get; set; }

        public PlayerState InitialPlayerState { get; set; }
    }

    //Manual serializable packets
    public struct SpawnPacket : INetSerializable
    {
        public long PlayerId;
        public Vector2 Position;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PlayerId);
            writer.Put(Position);
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = reader.GetLong();
            Position = reader.GetVector2();
        }
    }

    [Flags]
    public enum MovementKeys : byte
    {
        Left = 1 << 1,
        Right = 1 << 2,
        Up = 1 << 3,
        Down = 1 << 4,
        Fire = 1 << 5
    }
    
    public struct PlayerInputPacket : INetSerializable
    {
        public MovementKeys Keys;
        public float Rotation;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put((byte)Keys);
            writer.Put(Rotation);
        }

        public void Deserialize(NetDataReader reader)
        {
            Keys = (MovementKeys)reader.GetByte();
            Rotation = reader.GetFloat();
        }
    }
    
    public struct PlayerState : INetSerializable
    {
        public byte Id;
        public Vector2 Position;
        public float Rotation;
        public byte Health;

        private const float PI2 = Mathf.PI * 2f;
        private const float USHORT_MAX = UInt32.MaxValue;
        public const int Size = 1 + 8 + 2 + 1;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Position);
            //compress rotation to ushort
            writer.Put((ushort)( Rotation * USHORT_MAX / PI2 ));
            writer.Put(Health);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            Position = reader.GetVector2();
            Rotation = reader.GetUShort() * PI2 / USHORT_MAX;
            Health = reader.GetByte();
        }
    }

    public struct ServerState : INetSerializable
    {
        public ushort Tick;
        public PlayerState[] PlayerStates;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Tick);
            for (int i = 0; i < PlayerStates.Length; i++)
                PlayerStates[i].Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            Tick = reader.GetByte();
            int statesCount = reader.AvailableBytes / PlayerState.Size;
            if (PlayerStates == null)
                PlayerStates = new PlayerState[statesCount];
            else if(PlayerStates.Length < statesCount)
                Array.Resize(ref PlayerStates, statesCount);
            for (int i = 0; i < PlayerStates.Length; i++)
                PlayerStates[i].Deserialize(reader);
        }
    }
}