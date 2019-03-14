using LiteNetLib.Utils;
using UnityEngine;

namespace Code.Shared
{
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
        public long Id { get; set; }
        public bool NewPlayer { get; set; }
    }

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
    
    public struct MovementPacket : INetSerializable
    {
        public Vector2 Velocity;
        public float Rotation;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Velocity);
            writer.Put(Rotation);
        }

        public void Deserialize(NetDataReader reader)
        {
            Velocity = reader.GetVector2();
            Rotation = reader.GetFloat();
        }
    }
}