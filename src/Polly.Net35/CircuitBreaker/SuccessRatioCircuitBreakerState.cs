using System;
using Polly.Utilities;

namespace Polly.CircuitBreaker
{
    internal class SuccessRatioCircuitBreakerState : ICircuitBreakerState
    {
        private readonly TimeSpan _durationOfBreak;
        private readonly double _minSuccessRatio;
        private int _successCount;
        private int _failCount;
        private DateTime _blockedTill;
        private Exception _lastException;
        private readonly object _lock = new object();

        public SuccessRatioCircuitBreakerState(double minSuccessRatio, TimeSpan durationOfBreak)
        {
            _durationOfBreak = durationOfBreak;
            _minSuccessRatio = minSuccessRatio;
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
                _successCount += 1;
                Initialize();
            }
        }

        public void TryBreak(Exception ex)
        {
            using (TimedLock.Lock(_lock))
            {
                _lastException = ex;
                _failCount += 1;

                var currentSuccessRatio = ((double)_successCount / (_successCount + _failCount)) * 100;
                if (currentSuccessRatio < _minSuccessRatio)
                {
                    BreakTheCircuit();
                }
            }
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
        }

        private void Initialize()
        {
            _blockedTill = DateTime.MinValue;

            _lastException = new InvalidOperationException("This exception should never be thrown");
        }

    }
}