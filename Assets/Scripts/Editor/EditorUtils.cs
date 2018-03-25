using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public static class EditorUtils
{
    private static AnimatorController animator = Resources.Load<AnimatorController>("BaseAnimator");

    public static AnimatorControllerLayer GetDefaultLayer(this AnimatorController animator)
    {
        return animator.layers[0];
    }
    
    [MenuItem(itemName: "Machine/Import Submachine")]
    public static void ImportSubmachine()
    {
        Debug.Log("BaseAnimator state count: " + animator.GetDefaultLayer().stateMachine.states.Length);

        var stateMachine = animator.GetDefaultLayer().stateMachine;
        
        foreach (var subMachine in stateMachine.stateMachines)
        {
            ClearStateMachine(subMachine.stateMachine);

            var importedController = subMachine.stateMachine.behaviours[0] as SubMachineImporter;

            if (importedController == null)
            {
                continue;
            }

            var importedMachine = importedController.ImportedSubmachine.GetDefaultLayer().stateMachine;
            
            Debug.Log("Trying to import " + importedMachine.states.Length +
                      " states");
            
            AddStatesToStateMachine(subMachine.stateMachine, importedMachine.states);

            ImportParameters(animator, importedController.ImportedSubmachine);

            ClearTransitions(subMachine.stateMachine);
            
            ImportTransitions(subMachine.stateMachine, importedMachine.states);
        }
    }

    private static void ClearTransitions(AnimatorStateMachine stateMachine)
    {
        foreach (var state in stateMachine.states)
        {
            foreach (var transition in state.state.transitions)
            {
                state.state.RemoveTransition(transition);
            }            
        }
    }

    private static void ImportTransitions(AnimatorStateMachine stateMachine, ChildAnimatorState[] states)
    {
        var transitionEquality = new TransitionEqualityComparer();
        
        foreach (var state in states)
        {
            foreach (var transition in state.state.transitions)
            {
                var fromState = FindStateByName(stateMachine, state.state.name);
                var toState = FindStateByName(stateMachine, transition.destinationState.name);

                AnimatorStateTransition newTransition = new AnimatorStateTransition();
                newTransition.destinationState = toState;

                var originalCondition = transition.conditions[0];
                var conditionCopy = new AnimatorCondition();
                conditionCopy.mode = originalCondition.mode;
                conditionCopy.parameter = originalCondition.parameter;
                conditionCopy.threshold = originalCondition.threshold;
                
                newTransition.conditions = new AnimatorCondition[]
                {
                    conditionCopy
                };

                if (!fromState.transitions.Contains(newTransition, transitionEquality))
                {
                    fromState.AddTransition(newTransition);
                }
            }
        }
    }

    private static AnimatorState FindStateByName(AnimatorStateMachine stateMachine, string stateName)
    {
        foreach (var state in stateMachine.states)
        {
            if (state.state.name == stateName)
            {
                return state.state;
            }
        }
        return null;
    }

    private static void ImportParameters(AnimatorController destination, AnimatorController source)
    {
        foreach (var parameter in source.parameters)
        {
            if (!destination.parameters.Contains(parameter))
            {
                destination.AddParameter(parameter);    
            }
        }
    }

    private static void AddStatesToStateMachine(AnimatorStateMachine stateMachine, ChildAnimatorState[] states)
    {
        foreach (var state in states)
        {
            var stateCopy = AnimatorState.Instantiate(state.state);
            stateCopy.name = stateCopy.name.Replace("(Clone)", "");
            stateMachine.AddState(stateCopy, state.position);
        }
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        foreach (var state in stateMachine.states)
        {
            stateMachine.RemoveState(state.state);
        }
    }

    class TransitionEqualityComparer : IEqualityComparer <AnimatorStateTransition>
    {
        public bool Equals(AnimatorStateTransition x, AnimatorStateTransition y)
        {
            return x.destinationState.name == y.destinationState.name;
        }

        public int GetHashCode(AnimatorStateTransition obj)
        {
            return obj.GetHashCode();
        }
    }

}
