using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Client
{ 
    public class ClientPlayer : BasePlayer
    {
        public readonly long Id;
        private PlayerInputPacket _nextCommand;
        private readonly ClientLogic _clientLogic;
        private readonly PlayerInputPacket[] _predictionPlayerStates;
        private ushort _lastServerTick;
        
        public ClientPlayer(ClientLogic clientLogic, string name, long id) : base(name)
        {
            _predictionPlayerStates = new PlayerInputPacket[60];
            Id = id;
            _clientLogic = clientLogic;
        }

        public void SetInput(Vector2 velocity, float rotation, bool fire)
        {
            _nextCommand.Keys = 0;
            if(fire)
                _nextCommand.Keys |= MovementKeys.Fire;
            
            if (velocity.x < -0.1f)
                _nextCommand.Keys |= MovementKeys.Left;
            if (velocity.x > 0.1f)
                _nextCommand.Keys |= MovementKeys.Right;
            if (velocity.y < -0.1f)
                _nextCommand.Keys |= MovementKeys.Up;
            if (velocity.y > 0.1f)
                _nextCommand.Keys |= MovementKeys.Down;

            _nextCommand.Rotation = rotation;
        }
        
        public override void Update(float delta)
        {
            ApplyInput(_nextCommand, delta);
            _clientLogic.SendPacketSerializable(PacketType.Movement, _nextCommand, DeliveryMethod.ReliableOrdered);
            base.Update(delta);
        }
    }
}