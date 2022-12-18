#pragma once
#ifndef IOT_STATEMACHINE_H
#define IOT_STATEMACHINE_H

/*
  Copyright 2022 Xeno Innovations, Inc. DBA, SuessLabs
  Internet of Things State Machine (aka: Tiny State Machine)
  Author: Damian Suess
  Reviewed by: Jason Soll
  Date: 2022-12-13
*/

#include "State.h"

typedef void (*CallbackHandler) ();
////typedef bool (*TransitionCondition) ();

class State;
class StateMachine
{
private:

protected:
  bool _isInitialized = false;
  State* _currentState = NULL;
  State* _previousState = NULL;

  std::string _dotGraphViz = "";

  /// @brief Initialize the state machine
  /// @return True on success, false on failure.
  bool Initialize()
  {
    if (!_isInitialized)
    {
      _isInitialized = true;

      // auto initialState = PULL FIRST ITEM IN STACK
      // ret = Next(initialState);
    }
  }
public:
  friend class State;

  StateMachine();
  StateMachine(int initialStateId);

  ~StateMachine()
  {
    // free(...);
    // _timed = NULL;
  }

  // TOOD: Rename class, State, to "StateBuilder". Otherwise, we need to call this "AddState()"
  /// @brief Add state to the collection.
  static State AddState(int stateId, std::string name) // StateBuilder State()
  {
    // Actual Usage:
    //  return StateBuilder{};

    // TODO: Add state to the collection
    auto state = State(stateId, name);

    return NULL;  // CONSIDER: return true;
  }

  /// @brief Start the state machine
  /// @return True on success, false on failure.
  bool Start();

  /// @brief Reset the state machine back to the beginning.
  // void Reset();

  /// @brief Fire the specified state as the next
  /// @param stateId State enumeration id.
  /// @return True on success.
  bool Next(int stateId);   // ?? return Bool or StateId?

  /// @brief Fire the next default state
  /// @return True on success.
  bool Next();

  void WaitFor()
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
};

#endif
