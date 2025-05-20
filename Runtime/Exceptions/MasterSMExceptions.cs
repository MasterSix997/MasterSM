using System;

namespace MasterSM.Exceptions
{
    public class MasterSMException : Exception
    {
        public string Error;
        public string Context; 
        public string Reason;
        public string[] Solutions;

        public override string Message => ExceptionCreator.GetMessage(Error, Context, Reason, Solutions);

        public MasterSMException(string error, string context = null, string reason = null, params string[] solutions)
        {
            Error = error;
            Context = context;
            Reason = reason;
            Solutions = solutions;
        }
    }

    internal static class ExceptionCreator
    {
        public static string GetMessage(string message, string context = null, string reason = null, params string[] solutions)
        {
            var solutionsText = solutions.Length switch
            {
                0 => "",
                1 => $"\nPossible solution: {solutions[0]}",
                _ => "\nPossible solutions:"
            };
            if (solutions.Length > 1)
            {
                for (var i = 0; i < solutions.Length; i++)
                    solutionsText = string.Concat(solutionsText, $"\n{i}- {solutions[i]}");
            }
            
            return string.Concat(
                $"Error: {message}", 
                context != null ? $"\nContext: {context}" : "",
                reason != null ? $"\nReason: {reason}" : "",
                solutionsText);
        }

        public static MasterSMException PriorityManagerIdAlreadyExists<TStateId>(in TStateId stateId)
        {
            return new MasterSMException(
                $"State with id '{stateId}' already exists.", 
                "Adding state.", 
                "Each state must have a unique id and be added only once.");
        }

        public static MasterSMException SamePriority<TStateId>(in TStateId a, in TStateId b, int priority, string context = null)
        {
            return new MasterSMException(
                $"The state '{a.ToString()}' has the same priority '{priority}' as the state '{b.ToString()}'.", 
                context, 
                "Each state must have a unique priority.",
                "Change the priority of one of the states.");
        }

        public static MasterSMException IdNotFound<TStateId>(in TStateId stateId, string context = null)
        {
            return new MasterSMException(
                $"State with id '{stateId}' not found.",
                context);
        }

        public static MasterSMException IdIndexOutOfRange(int idIndex, int? maxLenght = null, string context = null)
        {
            return new MasterSMException(
                idIndex < 0 
                    ? $"Id index '{idIndex}', cannot be less than 0."
                    : $"Id index '{idIndex}', is out of range{(maxLenght.HasValue ? $" '{maxLenght}'" : "")}.",
                context);
        }

        public static MasterSMException PriorityResolverNotImplementProvider<TStateId>(string providerName, TStateId stateId)
        {
            return new MasterSMException(
                $"StatePriority from id type: '{stateId}', does not implement '{providerName}",
                "Accessing Priority/Group",
                "Not all StatePriority are implemented using priorities/groups, there are several other rules to define the priority of a state");
        }

        public static MasterSMException StateIdAlreadyExists<TStateId, TStateMachine>(TStateId id, IState<TStateId, TStateMachine> state) where TStateMachine : IStateMachine
        {
            return new MasterSMException(
                $"Failure when adding state '{state}', a state with the ID '{id}' already exists in the machine.",
                "Adding state",
                "Each state must have its own unique id. It is not allowed to have more than one state with the same id in a machine.",
                "Make sure any other state added previously already uses this id.",
                "You may have added the same state more than once."
            );
        }

        public static MasterSMException LayerIdAlreadyExists(object layerId)
        {
            return new MasterSMException(
                $"A layer with id '{layerId}' already exists in the machine.",
                "Adding layer",
                "Each layer must have its own unique id. It is not allowed to have more than one layer with the same id in a machine.",
                "Make sure the previously added layers do not have this id.",
                "You may have added the same layer more than once."
            );
        }

        public static MasterSMException LayerNotFound(object layerId, string context)
        {
            return new MasterSMException(
                $"Layer with id {layerId} does not exist.",
                context,
                null,
                "Make sure the id is correct.",
                "Make sure you actually added the layer.",
                "Make sure you do not remove the layer at some point"
            );
        }
    }
}