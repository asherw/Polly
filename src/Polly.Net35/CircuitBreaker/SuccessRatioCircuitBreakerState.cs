using System;
using Polly.Utilities;

namespace Polly.CircuitBreaker
{
    internal class SuccessRatioCircuitBreakerState : ICircuitBreakerState
    {
        private readonly TimeSpan _durationOfBreak;
        private readonly double _minSuccessRatio;
        private double _successCount;
        private double _failCount;
        private DateTime _lastSuccessUpdate;
        private DateTime _lastFailUpdate;

        private DateTime _blockedTill;
        private Exception _lastException;
        private readonly object _lock = new object();

        private readonly double _decayFactor;

        public SuccessRatioCircuitBreakerState(double minSuccessRatio, TimeSpan durationOfBreak, TimeSpan halfLife)
        {
            _durationOfBreak = durationOfBreak;
            _minSuccessRatio = minSuccessRatio;
            _decayFactor = Math.Log(0.5) / (TimeSpan.TicksPerSecond * halfLife.TotalSeconds);
            Initialize();
        }

        public Exception LastException
        {
            get
            {
                using (TimedLock.Lock(_lock))
                {
                    return _lastException;
                }
            }
        }

        public bool IsBroken
        {
            get
            {
                using (TimedLock.Lock(_lock))
                {
                    return SystemClock.UtcNow() < _blockedTill;
                }
            }
        }

        public void Reset()
        {
            using (TimedLock.Lock(_lock))
            {
                _successCount = Decay(_successCount, _lastSuccessUpdate);
                _successCount += 1;
                _lastSuccessUpdate = SystemClock.UtcNow();

                Initialize();
            }
        }

        public void TryBreak(Exception ex)
        {
            using (TimedLock.Lock(_lock))
            {

                _lastException = ex;
                _failCount = Decay(_failCount, _lastFailUpdate);
                _failCount += 1;
                _lastFailUpdate = SystemClock.UtcNow();

                var successDecay = Decay(_successCount, _lastSuccessUpdate);

                var currentSuccessRatio = (successDecay / (successDecay + _failCount)) * 100;
                if (currentSuccessRatio < _minSuccessRatio)
                {
                    BreakTheCircuit();
                }
            }
        }

        private double Decay(double counter, DateTime lastUpdate)
        {
            var now = SystemClock.UtcNow();

            if (counter > 0.00001)
            {
                counter *= Math.Exp((now - lastUpdate).Ticks * _decayFactor);
            }

            return counter;
        }

        private void BreakTheCircuit()
        {
            var currentUtc = SystemClock.UtcNow();

            var willDurationTakeUsPastDateTimeMaxValue = _durationOfBreak > DateTime.MaxValue - currentUtc;
            _blockedTill = willDurationTakeUsPastDateTimeMaxValue ?
                               DateTime.MaxValue :
                               currentUtc + _durationOfBreak;

            _successCount = 0;
            _failCount = 0;
            _lastSuccessUpdate = currentUtc;
            _lastFailUpdate = currentUtc;
        }

        private void Initialize()
        {
            _blockedTill = DateTime.MinValue;

            _lastException = new InvalidOperationException("This exception should never be thrown");
        }

    }
}