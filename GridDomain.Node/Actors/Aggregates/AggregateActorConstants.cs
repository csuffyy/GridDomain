namespace GridDomain.Node.Actors.Aggregates
{
    public static class AggregateActorConstants
    {
        public const string CreatedFault = "created fault";
        public const string CommandRaisedAnError = "command raised an error";
        public const string PublishingEvent = "Publishing event";
        public const string CommandExecutionCreatedAnEvent = "Command execution created an event";

        public const string ErrorOnEventApplyText = "Aggregate {id} raised errors on events apply after persist while executing command {@command}  \r\n" +
                                                    "State is supposed to be corrupted.  \r\n" +
                                                    "Events will be persisted.\r\n" +
                                                    "Aggregate will be stopped immediately, all pending commands will be dropped.";

        public const string ErrorOnContinuationText = "Aggregate {id} raised error while executing command {@command}. \r\n" +
                                                      "After some events produced and persisted, a continuation raises an error \r\n" +
                                                      "Current aggregate state will be taken as a new state. \r\n" +
                                                      "Aggregate is running and will execute futher commands";
    }
}