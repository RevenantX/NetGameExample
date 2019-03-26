using Code.Shared;
using UnityEngine;

namespace Code.Client
{   
    public class RemotePlayer : BasePlayer
    {
        struct IncomingData
        {
            public PlayerState State;
            public ushort Tick;

            public IncomingData(PlayerState state, ushort serverTick)
            {
                State = state;
                Tick = serverTick;
            }
        }
        private readonly LiteRingBuffer<IncomingData> _buffer = new LiteRingBuffer<IncomingData>(30);
        private float _receivedTime;
        private float _timer;
        private const float BufferTime = 0.1f; //100 milliseconds
        
        public RemotePlayer(ClientPlayerManager manager, string name, PlayerJoinedPacket pjPacket) : base(manager, name, pjPacket.InitialPlayerState.Id)
        {
            _position = pjPacket.InitialPlayerState.Position;
            _health = pjPacket.Health;
            _rotation = pjPacket.InitialPlayerState.Rotation;
            _buffer.Add(new IncomingData(pjPacket.InitialPlayerState, pjPacket.ServerTick));
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
            var dataA = _buffer[0];
            var dataB = _buffer[1];
            
            float lerpTime = NetworkGeneral.SeqDiff(dataB.Tick, dataA.Tick)*LogicTimer.FixedDelta;
            float t = _timer / lerpTime;
            _position = Vector2.Lerp(dataA.State.Position, dataB.State.Position, t);
            _rotation = Mathf.Lerp(dataA.State.Rotation, dataB.State.Rotation, t);
            _timer += delta;
            if (_timer > lerpTime)
            {
                _receivedTime -= lerpTime;
                _buffer.RemoveFromStart(1);
                _timer -= lerpTime;
            }
        }

        public void OnPlayerState(ushort serverTick, PlayerState state)
        {
            //old command
            int diff = NetworkGeneral.SeqDiff(serverTick, _buffer.Last.Tick);
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
            _buffer.Add(new IncomingData(state, serverTick));
        }
    }
}