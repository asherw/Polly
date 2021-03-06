﻿using System;
using System.Threading.Tasks;
using FluentAssertions;
using Polly.CircuitBreaker;
using Polly.Specs.Helpers;
using Polly.Utilities;
using Xunit;

namespace Polly.Specs
{
    public class CircuitBreakerAsyncSpecs : IDisposable
    {
        [Fact]
        public void Should_be_able_to_handle_a_duration_of_timespan_maxvalue()
        {
            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreakerAsync(1, TimeSpan.MaxValue);

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();
        }

        [Fact]
        public void Should_throw_if_exceptions_allowed_before_breaking_is_less_than_one()
        {
           Action action = () => Policy
                                    .Handle<DivideByZeroException>()
                                    .CircuitBreakerAsync(0, new TimeSpan());

            action.ShouldThrow<ArgumentOutOfRangeException>()
                  .And.ParamName.Should()
                  .Be("exceptionsAllowedBeforeBreaking");
        }

        [Fact]
        public void Should_open_circuit_with_the_last_raised_exception_after_specified_number_of_specified_exception_have_been_raised()
        {
            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>()
                  .WithMessage("The circuit is now open and is not allowing calls.")
                  .WithInnerException<DivideByZeroException>();
        }

        [Fact]
        public void Should_open_circuit_with_the_last_raised_exception_after_specified_number_of_one_of_the_specified_exceptions_have_been_raised()
        {
            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .Or<ArgumentOutOfRangeException>()
                            .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentOutOfRangeException>())
                  .ShouldThrow<ArgumentOutOfRangeException>();

            // 2 exception raised, cicuit is now open
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>()
                  .WithMessage("The circuit is now open and is not allowing calls.")
                  .WithInnerException<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Should_not_open_circuit_if_exception_raised_is_not_the_specified_exception()
        {
            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentNullException>())
                  .ShouldThrow<ArgumentNullException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentNullException>())
                  .ShouldThrow<ArgumentNullException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentNullException>())
                  .ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Should_not_open_circuit_if_exception_raised_is_not_one_of_the_the_specified_exceptions()
        {
            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .Or<ArgumentOutOfRangeException>()
                            .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentNullException>())
                  .ShouldThrow<ArgumentNullException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentNullException>())
                  .ShouldThrow<ArgumentNullException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentNullException>())
                  .ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Should_close_circuit_after_the_specified_duration_has_passed()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromMinutes(1);

            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreakerAsync(2, durationOfBreak);

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            // 2 exception raised, cicuit is now open
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // duration has passed, circuit now half open
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();
        }

        [Fact]
        public void Should_open_circuit_again_after_the_specified_duration_has_passed_if_the_next_call_raises_an_exception()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromMinutes(1);

            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreakerAsync(2, durationOfBreak);

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            // 2 exception raised, cicuit is now open
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // fist call after duration raises an exception, so circuit should break again
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();
        }


        [Fact]
        public async void Should_reset_circuit_after_the_specified_duration_has_passed_if_the_next_call_does_not_raise_an_exception()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromMinutes(1);

            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreakerAsync(2, durationOfBreak);

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            // 2 exception raised, cicuit is now open
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // fist call after duration is successful, so circuit should reset
            await policy.ExecuteAsync(() => Task.FromResult(0));

            // circuit has been reset so should once again allow 2 exceptions to be raised before breaking
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();
        }

        [Fact]
        public void Should_throw_when_oncircuitbreak_action_is_null()
        {
            Action<Exception> nullOnCircuitBroken = null;

            Action policy = () => Policy
                                      .Handle<DivideByZeroException>()
                                      .CircuitBreakerAsync(1, TimeSpan.MaxValue, nullOnCircuitBroken);

            policy.ShouldThrow<ArgumentNullException>().And
                  .ParamName.Should().Be("onCircuitBroken");
        }

        [Fact]
        public void Should_call_oncircuitbroken_upon_circuit_breaking_with_exception()
        {
            const string expectedException = "Exception";
            Exception thrownException = null;

            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreakerAsync(1, TimeSpan.FromMinutes(1), exception => thrownException = exception);

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>(1, (e, i) => e.HelpLink = "Exception"))
                  .ShouldThrow<DivideByZeroException>();

            thrownException.HelpLink.Should().Be(expectedException);
        }

        [Fact]
        public void Should_not_call_oncircuitbroken_before_circuit_is_broken()
        {
            Exception thrownException = null;

            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1), exception => thrownException = exception);

            // Only invoking exception once, though 2 exceptions are required to break circuit.
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            thrownException.Should().BeNull();
        }

        [Fact]
        public void Should_not_call_oncircuitbroken_after_circuit_is_broken()
        {
            int exceptionCounts = 0;

            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1), exception => exceptionCounts++);

            // First call will throw exception, but onCircuitBroken will not be called.
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            // Second call should break exception, calling onCircuitBroken.
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            // Third call should throw BrokenCiruit exception, but do not expect onCircuitBroken to be called again.
            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();

            exceptionCounts.Should().Be(1);
        }

        public void Dispose()
        {
            SystemClock.Reset();
        }
    }
}