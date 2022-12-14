#pragma once
#ifndef TINY_STATE_H
#define TINY_STATE_H

#include "State.h"

typedef void (*CallbackFunction) ();
typedef bool (*TransitionCondition) (); // Optional transitioning event

class TinyState
{
private:
protected:
public:
  TinyState();

  bool Add(Transition transition[], int size);
  bool Add(TimoutTransition transition[], int size);

  void InitialState(State* state);
  void FinishedHandler(CallbackFunction fctn);
  void TransitionHandler(CallbackFunction fctn);
};

#endif TINY_STATE_H
