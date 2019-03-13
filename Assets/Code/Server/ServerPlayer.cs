using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Server
{
    public class ServerPlayer : BasePlayer
    {
        private float _t;

        public readonly NetPeer AssociatedPeer;

        public ServerPlayer(NetPeer peer)
        {
            peer.Tag = this;
            AssociatedPeer = peer;
        }
        public override void Update(float delta)
        {
            base.Update(delta);
            
            //Draw rotating cross as server player
            const float sz = 0.1f;
            float sint = Mathf.Sin(Rotation);
            float cost = Mathf.Cos(Rotation);
            float siny = Position.y * sint;
            float cosx = Position.x * cost;
            float cosy = Position.y * cost;
            float sinx = Position.x * sint;
            Debug.DrawLine(
                new Vector2((Position.x - sz)*cost - siny, (Position.x - sz)*sint + cosy ),
                new Vector2((Position.x + sz)*cost - siny, (Position.x + sz)*sint + cosy ), 
                Color.green);
            Debug.DrawLine(
                new Vector2(cosx - (Position.y - sz)*sint, sinx + (Position.y - sz)*cost ),
                new Vector2(cosx - (Position.y + sz)*sint, sinx + (Position.y + sz)*cost ), 
                Color.green);
            
        }
    }
}