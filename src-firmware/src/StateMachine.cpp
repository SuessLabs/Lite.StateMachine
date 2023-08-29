/*
  Copyright 2022-2023 Xeno Innovations, Inc. DBA: Suess Labs
  Light (IoT) State Machine (aka: Tiny State Machine)
  Author: Damian Suess
  Reviewed by: Jason Soll
  Date: 2022-12-13
*/

#include "StateMachine.h"

bool StateMachine::Initialize()
{
  if (!_isInitialized)
  {
    _states.clear();

    _isInitialized = true;

    // auto initialState = PULL FIRST ITEM IN STACK
    // ret = Next(initialState);
  }

  return _isInitialized;
}

  // TOOD: Rename class, State, to "StateBuilder". Otherwise, we need to call this "AddState()"
  /// @brief Add state to the collection.
  State& StateMachine::AddState(int stateId, std::string name) // StateBuilder State()
  {
    // Actual Usage:
    //  return StateBuilder{};

    // TODO: Add state to the collection
    State* state(stateId, name);

    return state;  // CONSIDER: return true;
  }

void StateMachine::WaitFor()
{
  //// unsigned long now = millis();
  if (!_isInitialized)
    Initialize();

  if (_currentState != NULL)
  {
    ////if (_currentState->OnState != NULL)
    ////  _currentState->OnState();

    // The state timed out
    /*
      if (_currentState->HasTimeout() &&
          now >= _timeoutStarted + _timeoutDuration)
      {
      }
    */
  }
}