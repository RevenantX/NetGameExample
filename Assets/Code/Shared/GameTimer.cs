using System;

namespace Code.Shared
{
    public struct GameTimer
    {
        private float _maxTime;
        private float _time;

        public bool IsTimeElapsed => _time >= _maxTime;

        public float Time => _time;
        
        public float MaxTime
        {
            get => _maxTime;
            set => _maxTime = value;
        }
        
        public GameTimer(float maxTime)
        {
            _maxTime = maxTime;
            _time = 0f;
        }

        public void Reset()
        {
            _time = 0f;
        }

        public void UpdateAsCooldown(float delta)
        {
            _time += delta;
        }
        
        public void Update(float delta, Action onUpdate)
        {
            _time += delta;
            while (_time >= _maxTime)
            {
                _time -= _maxTime;
                onUpdate();
            }
        }
    }
}