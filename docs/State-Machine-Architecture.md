# State Machine Architecture

## Characteristics of a Composite State

* **Hierarchical Structure**: A composite state can be decomposed into lower-level states, which themselves can be composite states, creating a hierarchy.
* **Sequential (OR) States**: In this type, the composite state can transition to one of several substates, but only one can be active at a time.
* **Concurrent (AND) States**: This type uses regions within the composite state, where multiple processes run in parallel. A composite state with concurrent regions will have multiple substates active at the same time, one for each region.
* **Simplifies complexity**: By allowing states to be broken down, composite states help manage complex systems without having to represent every single state transition in one large, flat diagram.
* **Can contain history states**: A composite state can have a history state, which remembers the last substate that was active when the composite state was exited. A "shallow history" state remembers the last state on the same hierarchy level, while a "deep history" state remembers the last simple state visited at any level within the composite state.

### Examples

As an example:

* A "Robot" state machine could have a composite state called "Operating".
* The "Operating" state could be decomposed into two concurrent regions: one for "Control" and one for "Power Management".
* The "Control" region could contain substates like "Moving" and "Sensing," while the "Power Management" region could contain "Charging" and "Battery Saving".
