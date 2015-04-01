using System;
using FluentAssertions;
using Polly.CircuitBreaker;
using Polly.Specs.Helpers;
using Polly.Utilities;
using Xunit;

namespace Polly.Specs
{
    public class SuccessRatioCircuitBreakerSpecs : IDisposable
    {
        [Fact]
        public void HandleTimeSpanMaxValue()
        {
            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreaker(100.0, TimeSpan.MaxValue, TimeSpan.FromSeconds(30));

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();
        }

        [Fact]
        public void ThrowWhenMinSuccessRatioLessThanZero()
        {
           Action action = () => Policy
                                    .Handle<DivideByZeroException>()
                                    .CircuitBreaker(-1.0, new TimeSpan(), TimeSpan.FromSeconds(30));

            action.ShouldThrow<ArgumentOutOfRangeException>()
                  .And.ParamName.Should()
                  .Be("minSuccessRatio");
        }

        [Fact]
        public void OpenCircuitWhenBelowMinSuccessRatio()
        {
            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreaker(45.0, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30));

            policy.Invoking(x => x.Execute(() => { })).ShouldNotThrow();  

            policy.Invoking(x =>  x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>()
                  .WithMessage("The circuit is now open and is not allowing calls.")
                  .WithInnerException<DivideByZeroException>();
        }

        [Fact]
        public void DontOpenForUnHandledExceptions()
        {
            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreaker(100.0, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30));

            policy.Invoking(x => x.RaiseException<ArgumentNullException>())
                  .ShouldThrow<ArgumentNullException>();

            policy.Invoking(x => x.RaiseException<ArgumentNullException>())
                  .ShouldThrow<ArgumentNullException>();

            policy.Invoking(x => x.RaiseException<ArgumentNullException>())
                  .ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CloseCircuitAfterBreakDuration()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromMinutes(1);

            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreaker(50.0, durationOfBreak, TimeSpan.FromSeconds(30));

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            // 2 exception raised, cicuit is now open
            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // duration has passed, circuit now half open
            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();
        }

        [Fact]
        public void OpenWhenFirstCallAfterDurationFails()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromMinutes(1);

            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreaker(50.0, durationOfBreak, TimeSpan.FromSeconds(30));

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            // 2 exception raised, cicuit is now open
            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // fist call after duration raises an exception, so circuit should break again
            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();
        }


        [Fact]
        public void ResetWhenFirstCallAfterDurationSucceeds()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromMinutes(1);

            var policy = Policy
                            .Handle<DivideByZeroException>()
                            .CircuitBreaker(45.0, durationOfBreak, TimeSpan.FromSeconds(30));

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            // 2 exception raised, cicuit is now open
            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // fist call after duration is successful, so circuit should reset
            policy.Execute(() => { });

            // circuit has been reset so should once again allow 2 exceptions to be raised before breaking
            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<BrokenCircuitException>();
        }

        [Fact]
        public void VerifyDecayWorking()
        {
            var policy = Policy
            .Handle<DivideByZeroException>()
            .CircuitBreaker(99.0, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30));

            for (var i = 0; i < 1000; i++)
            {
                policy.Invoking(x => x.Execute(() => { })).ShouldNotThrow();  
            }

            SystemClock.UtcNow = () => DateTime.UtcNow.AddHours(3);

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            policy.Invoking(x => x.RaiseException<DivideByZeroException>())
              .ShouldThrow<BrokenCircuitException>();

        }

        public void Dispose()
        {
            SystemClock.Reset();
        }
    }
}