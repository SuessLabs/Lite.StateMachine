
public abstract class StateDefinition
{
    public StateId Id { get; }

    protected StateDefinition(StateId id) => Id = id;

    public virtual Task OnEntering(Context context) => Task.CompletedTask;
    public virtual Task OnEnter(Context context) => Task.CompletedTask;
    public virtual Task OnExit(Context context) => Task.CompletedTask;
    public virtual Task OnTimeout(Context context) => Task.CompletedTask;
    public virtual Task OnMessage(string message, Context context) => Task.CompletedTask;
}

public class CompositeState : StateDefinition
{
    public Dictionary<StateId, StateDefinition> SubStates { get; } = new();
    public StateId? InitialSubState { get; set; }
    public bool EnableHistory { get; set; } = false;
    private StateId? _lastActiveSubState;

    public CompositeState(StateId id) : base(id) { }

    public void AddSubState(StateDefinition state) => SubStates[state.Id] = state;

    public StateId GetNextSubState() => EnableHistory && _lastActiveSubState.HasValue
        ? _lastActiveSubState.Value
        : InitialSubState ?? throw new InvalidOperationException("No initial sub-state defined.");

    public void SetLastActive(StateId subStateId)
    {
        if (EnableHistory) _lastActiveSubState = subStateId;
    }
}


// SAMPLE
public class LoadingState : StateDefinition
{
    public LoadingState() : base(StateId.Loading) { }

    public override async Task OnEnter(Context context)
    {
        Console.WriteLine("Loading...");
        await Task.Delay(500); // Simulate work
    }

    public override Task OnTimeout(Context context)
    {
        Console.WriteLine("Loading timed out.");
        return Task.CompletedTask;
    }

    public override Task OnExit(Context context)
    {
        Console.WriteLine("Exiting Loading.");
        return Task.CompletedTask;
    }
}

public class SubProcessingState : StateDefinition
{
    public SubProcessingState() : base(StateId.SubProcessing) { }

    public override Task OnEnter(Context context)
    {
        Console.WriteLine("Sub-processing started.");
        return Task.CompletedTask;
    }

    public override Task OnMessage(string message, Context context)
    {
        Console.WriteLine($"SubProcessing received: {message}");
        return Task.CompletedTask;
    }
}
