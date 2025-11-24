## MK-3

Using C#, create a finite state machine which can optionally have composite states, with each state defined as an Enum. States must be lazy-loaded. Defining states must use the State enumeration and a human-readable name. Registering states uses the method named, "State"

Each state's transition must be an async method. The state can has an optional transition of OnEntering for transitioning into it, OnEnter for when fully transitioned, optionally an OnMessage for receiving messages sent by the OnEnter, optionally an OnTimeout for when events are not received by the OnMessage in time, and optionally an OnExit for when it is completed.

Each transition must pass a Context class as an argument which contains a property named "Params" of type "Dictionary<string, object>", property named "Errors" of type "Dictionary<string, object>", a property "LastState" which has the enum value of the previous state, and the method "NextState" to trigger moving to the next state. The "NextState" method has an enum argument named "Result" with the values of "Success", "Error", and "Failure". The NextState method can be called by any of the transitions to move to the next state.


### Suggestions

Package this into a reusable library-style class. Extend this design to support composite states explicitly

## MK2 - Redesign with Messaging, Timeouts and Context


Using C# create a finite state machine which is async and can have composite states. States are defined by an enumeration value. Each state passes a context payload in the form a Dictionary<string, object>. Each state has an optional OnEntering for transitioning into it,  OnEnter for when fully transitioned, optionally an OnMessage for receiving message events sent by the OnEnter, optionally an OnTimeout for when events are not received by the OnMessage in time, and optionally an OnExit for when it is completed.

Extend to fully support composite states with hierarchical transitions, and add a message queue for event-driven behavior.

Make the context "Dictionary<string, object>" defined as class, "public class Context : Dictionary<string, object>;"

Add state history tracking.  package this into a reusable library with interfaces and DI. Name the library, "LiteState".

Make state history tracking optional. Provide a sample program using all variations of the features. Add unit tests for LiteState using MSTest.

**
package this into a NuGet-ready library with interfaces, DI support, and XML documentation comments

Generate the full solution as a ZIP file

Make state's OnEntering, OnEnter, OnTimeout, OnMessage, and OnExit as methods