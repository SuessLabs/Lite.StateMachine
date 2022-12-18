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
  bool Initialize();
public:
  StateMachine();
  StateMachine(int initialStateId);

  /// @brief Add state to the collection.
  void State()
  {
    // Add state to the collection
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
};

#endif
