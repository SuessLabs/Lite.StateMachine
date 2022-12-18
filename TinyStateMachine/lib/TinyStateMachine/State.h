#pragma once

#ifndef STATE_H
#define STATE_H

#include <string>
//#include <vector>
#include <list>

typedef void (*CallbackHandler) ();
////typedef bool (*TransitionCondition) ();

// ? rename to 'StateBuilder'?
class State
{
  // Allow calling the Iot StateMachine class
  friend class StateMachine;

private:
protected:
  int _id;
  static int _nextId;
  std::string _name = "";

  int _defaultNextState;
  //std::vector<int> _nextStates;
  std::list<int> _nextStates;
  int _timeoutDuration;

  CallbackHandler OnEnterHandler = NULL;
  //CallbackHandler OnMessageHandler = NULL;
  CallbackHandler OnTimeoutHandler = NULL;
  CallbackHandler OnExitHandler = NULL;
  //CallbackHandler OnErrorHandler = NULL;

public:
  State()
  {
    _nextStates.clear();
    _timeoutDuration = 0;
  }

  State(int stateId,
        std::string name,
        CallbackHandler onEnter,
        // CallbackHandler onTimeout = NULL,
        CallbackHandler onExit = NULL,
        int msTimeout = 0);
        // bool isFinal = false);

  State(int stateId, std::string name)
  {
    _id = stateId;
    _name = name;
  }

  State& AllowNext(const int nextStateId, bool isDefault = false)
  {
    _nextStates.push_back(nextStateId);

    if (_nextStates.size() == 0 || isDefault)
      _defaultNextState = nextStateId;

    return *this;
  }

  State& OnEnter(CallbackHandler methodHandler)
  {
    this->OnEnterHandler = methodHandler;
    return *this;
  }

  // State& OnMessage(CallbackHandler methodHandler);
  // State& OnTimeout(CallbackHandler methodHandler, int msTimeout);

  State& OnTimeout(CallbackHandler methodHandler, int msTimeout)
  {
    this->_timeoutDuration = msTimeout;
    this->OnTimeoutHandler = methodHandler;
    return *this;
  }

  State& OnExit(CallbackHandler methodHandler)
  {
    this->OnExitHandler = methodHandler;
    return *this;
  }

  ~State();

  void Setup(std::string name,
             CallbackHandler onEnter,
             CallbackHandler onHandle,
             CallbackHandler onExit,
             bool isFinal = false);

  void Name(std::string name) { _name = name; }
  std::string Name() const { return _name; }

  int Id() const { return _id; }

  //// bool IsFinal() const { return _isFinal; }
};

#endif
