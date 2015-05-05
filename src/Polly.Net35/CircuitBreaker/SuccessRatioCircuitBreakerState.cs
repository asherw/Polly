using System;
using Polly.Utilities;

namespace Polly.CircuitBreaker
{
    /// <summary>
    /// 
    /// </summary>
    public class SuccessRatioCircuitBreakerState : ICircuitBreakerState
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minSuccessRatio"></param>
        /// <param name="durationOfBreak"></param>
        /// <param name="halfLife"></param>
        public SuccessRatioCircuitBreakerState(double minSuccessRatio, TimeSpan durationOfBreak, TimeSpan halfLife)
        {
            _durationOfBreak = durationOfBreak;
            _minSuccessRatio = minSuccessRatio;
            _decayFactor = Math.Log(0.5) / (TimeSpan.TicksPerSecond * halfLife.TotalSeconds);
            Initialize();
        }

        /// <summary>
        /// Last exception handled.
        /// </summary>
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

        /// <summary>
        /// Get current state of the circuit.
        /// </summary>
        /// <returns>bool that states wether the circuit is broken or not.</returns>
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

        /// <summary>
        /// Reset the state of the circuitbreaker.
        /// </summary>
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

        /// <summary>
        /// Used by Polly to try to break the circuit. If an exception is recieved that matches the policy, then Polly will call this method to try to break the state of the circuit.
        /// </summary>
        /// <param name="ex"></param>
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