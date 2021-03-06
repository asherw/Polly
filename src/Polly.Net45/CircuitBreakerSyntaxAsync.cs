﻿using System;
using Polly.CircuitBreaker;

namespace Polly
{
    /// <summary>
    /// Fluent API for defining a Circuit Breaker <see cref="Policy"/>. 
    /// </summary>
    public static class CircuitBreakerSyntaxAsync
    {
        /// <summary>
        /// <para> Builds a <see cref="Policy"/> that will function like a Circuit Breaker.</para>
        /// <para>The circuit will break after <paramref name="exceptionsAllowedBeforeBreaking"/>
        /// exceptions that are handled by this policy are raised. The circuit will stay
        /// broken for the <paramref name="durationOfBreak"/>. Any attempt to execute this policy
        /// while the circuit is broken, will immediately throw a <see cref="BrokenCircuitException"/> containing the exception 
        /// that broke the cicuit.
        /// </para>
        /// <para>If the first action after the break duration period results in an exception, the circuit will break
        /// again for another <paramref name="durationOfBreak"/>, otherwise it will reset.
        /// </para>
        /// </summary>
        /// <param name="policyBuilder">The policy builder.</param>
        /// <param name="exceptionsAllowedBeforeBreaking">The number of exceptions that are allowed before opening the circuit.</param>
        /// <param name="durationOfBreak">The duration the circuit will stay open before resetting.</param>
        /// <returns>The policy instance.</returns>
        /// <remarks>(see "Release It!" by Michael T. Nygard fi)</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">exceptionsAllowedBeforeBreaking;Value must be greater than zero.</exception>
        public static Policy CircuitBreakerAsync(this PolicyBuilder policyBuilder, int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak)
        {
            Action<Exception> doNothing = _ => { };

            return policyBuilder.CircuitBreakerAsync(exceptionsAllowedBeforeBreaking, durationOfBreak, doNothing);
        }

        /// <summary>
        /// <para> Builds a <see cref="Policy"/> that will function like a Circuit Breaker.</para>
        /// <para>The circuit will break after <paramref name="exceptionsAllowedBeforeBreaking"/>
        /// exceptions that are handled by this policy are raised. The circuit will stay
        /// broken for the <paramref name="durationOfBreak"/>. Any attempt to execute this policy
        /// while the circuit is broken, will immediately throw a <see cref="BrokenCircuitException"/> containing the exception 
        /// that broke the cicuit.
        /// </para>
        /// <para>If the first action after the break duration period results in an exception, the circuit will break
        /// again for another <paramref name="durationOfBreak"/>, otherwise it will reset.
        /// </para>
        /// </summary>
        /// <param name="policyBuilder">The policy builder.</param>
        /// <param name="exceptionsAllowedBeforeBreaking">The number of exceptions that are allowed before opening the circuit.</param>
        /// <param name="durationOfBreak">The duration the circuit will stay open before resetting.</param>
        /// <param name="onCircuitBroken">The action to call upon the circuit breaking.</param>
        /// <returns>The policy instance.</returns>
        /// <remarks>(see "Release It!" by Michael T. Nygard fi)</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">exceptionsAllowedBeforeBreaking;Value must be greater than zero.</exception>
        /// <exception cref="System.ArgumentNullException">onCircuitBroken</exception>
        public static Policy CircuitBreakerAsync(this PolicyBuilder policyBuilder, int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak, Action<Exception> onCircuitBroken)
        {
            if (exceptionsAllowedBeforeBreaking <= 0) throw new ArgumentOutOfRangeException("exceptionsAllowedBeforeBreaking", "Value must be greater than zero.");
            if (onCircuitBroken == null) throw new ArgumentNullException("onCircuitBroken");

            var policyState = new CircuitBreakerState(exceptionsAllowedBeforeBreaking, durationOfBreak, onCircuitBroken);
            return new Policy(action => CircuitBreakerPolicy.ImplementationAsync(action, policyBuilder.ExceptionPredicates, policyState));
        }
    }
}