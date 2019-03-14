using System;
using System.Diagnostics;

namespace Code.Shared
{
    public class LogicTimer
    {
        public const float FramesPerSecond = 60.0f;
        public const float FixedDelta = 1.0f / FramesPerSecond;
        private const long FixedDeltaNano = 15000 * 1000L;
        private const long NanoSecond = 1000000000L;

        private long _accumulator;
        private long _lastTime;

        private readonly Stopwatch _stopwatch;
        private readonly Action _action;

        public float LerpAlpha
        {
            get
            {
                return (_accumulator / (float)FixedDeltaNano);
            }
        }

        public LogicTimer(Action action)
        {
            _stopwatch = new Stopwatch();
            _action = action;
        }

        public void Start()
        {
            _lastTime = 0;
            _accumulator = 0;
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public void Update()
        {
            long elapsedTicks = _stopwatch.ElapsedTicks;
            _accumulator += (elapsedTicks - _lastTime)*NanoSecond/Stopwatch.Frequency;
            _lastTime = elapsedTicks;

            while (_accumulator >= FixedDeltaNano)
            {
                _action();
                _accumulator -= FixedDeltaNano;
            }
        }
    }
}