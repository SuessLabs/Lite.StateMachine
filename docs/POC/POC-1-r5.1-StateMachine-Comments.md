## Core

Add comments to code

```cs
/// <summary>
/// Represents a composite state that can contain sub-states and optionally track history.
/// </summary>
public class CompositeState : StateDefinition, ICompositeState
{
    /// <summary>
    /// Gets or sets the initial sub-state to enter when this composite state is activated.
    /// </summary>
    public StateId? InitialSubState { get; set; }

    /// <summary>
    /// Enables or disables history tracking for sub-states.
    /// </summary>
    public bool EnableHistory { get; set; } = false;

    /// <summary>
    /// Adds a sub-state to this composite state.
    /// </summary>
    /// <param name="state">The sub-state to add.</param>
    public void AddSubState(StateDefinition state) => SubStates[state.Id] = state;
}


/// <summary>
/// Extension methods for registering LiteState in DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers LiteState's AsyncStateMachine as a singleton service.
    /// </summary>
    public static IServiceCollection AddLiteState(this IServiceCollection services)
    {
        return services.AddSingleton<IStateMachine, AsyncStateMachine>();
    }
}
```
