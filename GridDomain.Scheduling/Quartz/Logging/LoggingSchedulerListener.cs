using GridDomain.Logging;
using Quartz;
using Serilog;

namespace GridDomain.Scheduling.Quartz.Logging
{
    public class LoggingSchedulerListener : ILoggingSchedulerListener
    {
        private readonly ILogger _log;

        public LoggingSchedulerListener(ILogger log)
        {
            _log = log.ForContext<LoggingSchedulerListener>();
        }
        public void JobScheduled(ITrigger trigger)
        {
            _log.Verbose("Job {JobKey} scheduled for next execution {NextFireTime}", trigger.JobKey, trigger.GetNextFireTimeUtc());
        }

        public void JobUnscheduled(TriggerKey triggerKey)
        {
            _log.Verbose("Trigger {TriggerKey} unscheduled ", triggerKey);
        }

        public void TriggerFinalized(ITrigger trigger)
        {
            _log.Verbose("Trigger {TriggerKey}  for job {JobKey} finalized and won`t fire again", trigger.Key,trigger.JobKey );
        }

        public void TriggerPaused(TriggerKey triggerKey)
        {
            _log.Verbose("Trigger {TriggerKey} paused", triggerKey);
        }

        public void TriggersPaused(string triggerGroup)
        {
            _log.Verbose("Triggers in group {TriggerGroup} paused", triggerGroup);
        }

        public void TriggerResumed(TriggerKey triggerKey)
        {
            _log.Verbose("Trigger {TriggerKey} resumed", triggerKey);
        }

        public void TriggersResumed(string triggerGroup)
        {
            _log.Verbose("Triggers in group {TriggerGroup} resumed", triggerGroup);
        }

        public void JobAdded(IJobDetail jobDetail)
        {
            _log.Verbose("Job {JobKey} added", jobDetail.Key);
        }

        public void JobDeleted(JobKey jobKey)
        {
            _log.Verbose("Job {JobKey} deleted", jobKey);
        }

        public void JobPaused(JobKey jobKey)
        {
            _log.Verbose("Job {JobKey} paused", jobKey);
        }

        public void JobsPaused(string jobGroup)
        {
            _log.Verbose("Jobs in group {JobGroup} paused", jobGroup);
        }

        public void JobResumed(JobKey jobKey)
        {
            _log.Verbose("Job {JobKey} resumed", jobKey);
        }

        public void JobsResumed(string jobGroup)
        {
            _log.Verbose("Jobs in group {JobGroup} resumed", jobGroup);
        }

        public void SchedulerError(string msg, SchedulerException cause)
        {
            _log.Error(cause, "Scheduler error {@message}",msg);
        }

        public void SchedulerInStandbyMode()
        {
            _log.Verbose("Scheduler goes to stand by mode");
        }

        public void SchedulerStarted()
        {
            _log.Verbose("Scheduler started");
        }

        public void SchedulerStarting()
        {
            _log.Verbose("Scheduler starting");
        }

        public void SchedulerShutdown()
        {
            _log.Verbose("Scheduler shut down");
        }

        public void SchedulerShuttingdown()
        {
            _log.Verbose("Scheduler shutting down");
        }

        public void SchedulingDataCleared()
        {
            _log.Verbose("Scheduling data cleared");
        }
    }
}