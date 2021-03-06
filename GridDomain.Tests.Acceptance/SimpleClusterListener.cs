﻿using Akka.Actor;
using Akka.Cluster;
using Akka.Event;

namespace GridDomain.Tests.Acceptance
{
    public class SimpleClusterListener : UntypedActor
    {
        protected Cluster Cluster = Cluster.Get(Context.System);
        protected ILoggingAdapter Log = Context.GetLogger();

        /// <summary>
        ///     Need to subscribe to cluster changes
        /// </summary>
        protected override void PreStart()
        {
            Cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents,
                new[] {typeof (ClusterEvent.IMemberEvent), typeof (ClusterEvent.UnreachableMember)});
        }

        /// <summary>
        ///     Re-subscribe on restart
        /// </summary>
        protected override void PostStop()
        {
            Cluster.Unsubscribe(Self);
        }

        protected override void OnReceive(object message)
        {
            var up = message as ClusterEvent.MemberUp;
            if (up != null)
            {
                var mem = up;
                Log.Warning("Member is Up: {0}", mem.Member);
            }
            else if (message is ClusterEvent.UnreachableMember)
            {
                var unreachable = (ClusterEvent.UnreachableMember) message;
                Log.Warning("Member detected as unreachable: {0}", unreachable.Member);
            }
            else if (message is ClusterEvent.MemberRemoved)
            {
                var removed = (ClusterEvent.MemberRemoved) message;
                Log.Warning("Member is Removed: {0}", removed.Member);
            }
            else if (message is ClusterEvent.IMemberEvent)
            {
                //IGNORE                
            }
            else if (message is ClusterEvent.CurrentClusterState)
            {
            }
            else
            {
                Unhandled(message);
            }
        }
    }
}