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
  State(std::string name,
            CallbackFunction onEnter,
            CallbackFunction onHandle = NULL,
            CallbackFunction onExit = NULL,
            bool isFinal = false);
  ~State();

  void Setup(std::string name,
             CallbackFunction onEnter,
             CallbackFunction onHandle,
             CallbackFunction onExit,
             bool isFinal = false);

  void Name(std::string name);
  void OnEnter(CallbackFunction method);
  void OnHandle(CallbackFunction method);
  // void OnMessage(CallbackFunction method);
  void OnExit(CallbackFunction method);

  int Id() const;
  bool IsFinal() const;
};

#endif
