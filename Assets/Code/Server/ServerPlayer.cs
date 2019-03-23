using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Server
{
    public class ServerPlayer : BasePlayer
    {
        public readonly NetPeer AssociatedPeer;
        public PlayerState NetworkState;
        
        public ServerPlayer(string name, NetPeer peer) : base(name)
        {
            peer.Tag = this;
            AssociatedPeer = peer;
            NetworkState = new PlayerState {Id = (byte) peer.Id};
        }

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            NetworkState.ProcessedCommandId = command.Id;
            base.ApplyInput(command, delta);
        }

        public override void Update(float delta)
        {
            base.Update(delta);
            NetworkState.Position = _position;
            NetworkState.Rotation = _rotation;
            NetworkState.Health = _health;
            
            //Draw rotating cross as server player
            const float sz = 0.1f;
            Debug.DrawLine(
                new Vector2(Position.x - sz, Position.y ),
                new Vector2(Position.x + sz, Position.y ), 
                Color.green);
            Debug.DrawLine(
                new Vector2(Position.x, Position.y - sz ),
                new Vector2(Position.x, Position.y + sz ), 
                Color.green);
            
        }
    }
}