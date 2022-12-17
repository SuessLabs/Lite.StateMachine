#pragma once

#ifndef STATE_H
#define STATE_H

#include <string>

typedef void (*CallbackFunction) ();
////typedef bool (*GuardCondition) ();

class State
{
  // friend class TinyStates;
private:
protected:
  int _id;
  static int _nextId;
  std::string _name = "";

  CallbackFunction OnEnter = NULL;
  CallbackFunction OnHandle = NULL;
  CallbackFunction OnExit = NULL;

public:
  State();
  State(int stateId,
        std::string name,
        CallbackFunction onEnter,
        // CallbackFunction onHandle = NULL,
        CallbackFunction onExit = NULL,
        int msTimeout = 0,
        bool isFinal = false);

  State(int stateId, std::string name);

  State& AllowNext(int nextStateId);
  State& OnEnter(CallbackFunction methodHandler);
  // State& OnMessage(CallbackFunction methodHandler);
  // State& OnTimeout(CallbackFunction methodHandler, int msTimeout);
  State& OnExit(CallbackFunction methodHandler);

  ~State();

  void Setup(std::string name,
             CallbackFunction onEnter,
             CallbackFunction onHandle,
             CallbackFunction onExit,
             bool isFinal = false);

  void Name(std::string name);
  void SetOnEnter(CallbackFunction method);
  // void SetOnHandle(CallbackFunction method);
  // void SetOnMessage(CallbackFunction method);
  void SetOnExit(CallbackFunction method);

  int Id() const;
  bool IsFinal() const;
};

#endif
