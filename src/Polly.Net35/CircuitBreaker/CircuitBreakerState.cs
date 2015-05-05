using System;
using Polly.Utilities;

namespace Polly.CircuitBreaker
{
    /// <summary>
    /// Interface used to describe the needed functionality needed for the circuitbreaker pattern.
    /// </summary>
    public class CircuitBreakerState : ICircuitBreakerState
    {
        private readonly TimeSpan _durationOfBreak;
        private readonly int _exceptionsAllowedBeforeBreaking;
        private readonly Action<Exception> _onCircuitBroken;

        private int _count;
        private DateTime _blockedTill;
        private Exception _lastException;
        private readonly object _lock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exceptionsAllowedBeforeBreaking"></param>
        /// <param name="durationOfBreak"></param>
        /// <param name="onCircuitBroken"></param>
        public CircuitBreakerState(int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak, Action<Exception> onCircuitBroken)
        {
            _durationOfBreak = durationOfBreak;
            _exceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            _onCircuitBroken = onCircuitBroken;

            Reset();
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
                _count = 0;
                _blockedTill = DateTime.MinValue;

                _lastException = new InvalidOperationException("This exception should never be thrown");
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

                _count += 1;
                if (_count >= _exceptionsAllowedBeforeBreaking)
                {
                    BreakTheCircuit();

                    if (_onCircuitBroken != null)
                        _onCircuitBroken(ex);
                }
            }
        }

        void BreakTheCircuit()
        {
            var willDurationTakeUsPastDateTimeMaxValue = _durationOfBreak > DateTime.MaxValue - SystemClock.UtcNow();
            _blockedTill = willDurationTakeUsPastDateTimeMaxValue ?
                               DateTime.MaxValue :
                               SystemClock.UtcNow() + _durationOfBreak;
        }
    }
}