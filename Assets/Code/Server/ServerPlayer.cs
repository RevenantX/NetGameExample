using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Server
{
    public class ServerPlayer : BasePlayer
    {
        private float _t;

        public readonly NetPeer AssociatedPeer;

        public ServerPlayer(string name, NetPeer peer) : base(name)
        {
            peer.Tag = this;
            AssociatedPeer = peer;
        }
        public override void Update(float delta)
        {
            base.Update(delta);
            
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