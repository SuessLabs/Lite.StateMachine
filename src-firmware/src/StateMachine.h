#ifndef IOT_STATEMACHINE_H
#define IOT_STATEMACHINE_H

// #pragma once

/*
  Copyright 2022-2023 Xeno Innovations, Inc. DBA: Suess Labs
  Light (IoT) State Machine (aka: Tiny State Machine)
  Author: Damian Suess
  Reviewed by: Jason Soll
  Date: 2022-12-13
*/

#include "State.h"
// #include <string>
// #include <list>
// #include <map>

using namespace std;

typedef void (*CallbackHandler) ();
// typedef bool (*TransitionCondition) (); // Optional state transitioning guard event handler

// class State;
class StateMachine
{
private:

protected:
  bool _isInitialized = false;
  State* _currentState = nullptr;
  State* _previousState = nullptr;
  std::list<State> _states;  // Consider using a `map<int, State>` or `vector<State>`
  // std::map<int, State> _stateMap;

  std::string _dotGraphViz = "";

  /// @brief Initialize the state machine
  /// @return True on success, false on failure.
  bool Initialize();

public:
  friend class State;

  StateMachine();
  StateMachine(int initialStateId);

  ~StateMachine()
  {
    // free(...);
    // _timed = nullptr;
  }

  // TOOD: Rename class, State, to "StateBuilder". Otherwise, we need to call this "AddState()"
  /// @brief Add state to the collection.
  State& AddState(int stateId, std::string name); // StateBuilder State()

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

  void WaitFor();
};

#endif
