using System.Collections.Generic;
using Code.Shared;
using UnityEngine;

namespace Code.Client
{
    public class RemotePlayer : BasePlayer
    {
        private readonly LiteRingBuffer<PlayerState> _buffer = new LiteRingBuffer<PlayerState>(30);
        private float _receivedTime;
        private float _timer;
        private const float BufferTime = 0.2f; //200 milliseconds
        
        public RemotePlayer(string name, PlayerState pjPacket) : base(name)
        {
            _position = pjPacket.Position;
            _health = pjPacket.Health;
            _rotation = pjPacket.Rotation;
            _buffer.Add(pjPacket);
        }

        public override void Spawn(Vector2 position)
        {
            _buffer.FastClear();
            base.Spawn(position);
        }

        public void UpdatePosition(float delta)
        {
            if (_receivedTime < BufferTime || _buffer.Count < 2)
                return;
            var stateA = _buffer[0];
            var stateB = _buffer[1];
            
            float lerpTime = NetworkGeneral.SeqDiff(stateB.ProcessedCommandId, stateA.ProcessedCommandId)*LogicTimer.FixedDelta;
            float t = _timer / lerpTime;
            _position = Vector2.Lerp(stateA.Position, stateB.Position, t);
            _rotation = Mathf.Lerp(stateA.Rotation, stateB.Rotation, t);
            _timer += delta;
            if (_timer > lerpTime)
            {
                _receivedTime -= lerpTime;
                _buffer.RemoveFromStart(1);
                _timer -= lerpTime;
            }
        }

        public void OnPlayerState(PlayerState state)
        {
            //old command
            int diff = NetworkGeneral.SeqDiff(state.ProcessedCommandId, _buffer.Last.ProcessedCommandId);
            if (diff <= 0)
                return;

            _receivedTime += diff * LogicTimer.FixedDelta;
            if (_buffer.IsFull)
            {
                Debug.LogWarning("[C] Remote: Something happened");
                //Lag?
                _receivedTime = 0f;
                _buffer.FastClear();
            }
            _buffer.Add(state);
        }
    }
}