// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Lite.StateMachine.BenchmarkTests.States;
using Microsoft.VSDiagnostics;

namespace Lite.StateMachine.BenchmarkTests;

[CPUUsageDiagnoser]
[MemoryDiagnoser]
public class BasicStateBenchmarks
{
  private StateMachine<BasicStateId> _machine = new();

  ////[Params(1, 100, 1_000, 10_000, 100_000, 1_000_000)]
  [Params(1, 100, 1_000, 10_000)]
  public int CyclesBeforeExit { get; set; }

  [GlobalSetup]
  public void BasicStateGlobalSetup()
  {
    // We will continue to loop using OnError transition until we reached our max counter
    // upon which, we will OnSuccess and exit the state machine.
    _machine = new StateMachine<BasicStateId>(null, null, isContextPersistent: true);
    _machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2);
    _machine.RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3);
    _machine.RegisterState<BasicState3>(BasicStateId.State3, onSuccess: null, onError: BasicStateId.State1);
  }

  [Benchmark]
  public async Task BasicStatesRunsAsync()
  {
    var maxCounter = CyclesBeforeExit;
    _machine.Context.Parameters = new()
    {
      { ParameterType.MaxCounter, maxCounter },
      { ParameterType.Counter, 0 },
    };

    await _machine.RunAsync(BasicStateId.State1);
  }

  [Benchmark]
  public void BasicStatesRunsSync()
  {
    var maxCounter = CyclesBeforeExit;
    _machine.Context.Parameters = new()
    {
      { ParameterType.MaxCounter, maxCounter },
      { ParameterType.Counter, 0 },
    };

    _machine.RunAsync(BasicStateId.State1)
            .GetAwaiter()
            .GetResult();
  }
}
