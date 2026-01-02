// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Lite.StateMachine.BenchmarkTest.States;
using Microsoft.VSDiagnostics;

namespace Lite.StateMachine.BenchmarkTest;

//// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet

////[ShortRunJob]
[CPUUsageDiagnoser]
[MemoryDiagnoser]
public class Benchmarks
{
  ////private SHA256 _sha256 = SHA256.Create();
  ////private byte[] _data = [];

  private StateMachine<BasicStateId> _machine = new();

  [Params(1, 100, 1_000, 10_000, 100_000, 1_000_000)]
  public int CyclesBeforeExit { get; set; }

  [GlobalSetup]
  public void BenchmarkSetup()
  {
    // We will continue to loop using OnError transition until we reached our max counter
    // upon which, we will OnSuccess and exit the state machine.
    _machine = new StateMachine<BasicStateId>(null, null, isContextPersistent: true);
    _machine.RegisterState<FlatState1>(BasicStateId.State1, BasicStateId.State2);
    _machine.RegisterState<FlatState2>(BasicStateId.State2, BasicStateId.State3);
    _machine.RegisterState<FlatState3>(BasicStateId.State3, onSuccess: null, onError: BasicStateId.State1);
  }

  [Benchmark]
  public async Task FlatStateMachineRunsAsync()
  {
    var maxCounter = CyclesBeforeExit;
    PropertyBag parameters = new()
    {
      { ParameterType.MaxCounter, maxCounter },
      { ParameterType.Counter, 0 },
    };

    await _machine.RunAsync(BasicStateId.State1, parameters);
  }

  [Benchmark]
  public void FlatStateMachineRunsSync()
  {
    var maxCounter = CyclesBeforeExit;
    PropertyBag parameters = new()
    {
      { ParameterType.MaxCounter, maxCounter },
      { ParameterType.Counter, 0 },
    };

    _machine.RunAsync(BasicStateId.State1).GetAwaiter().GetResult();
  }
}
