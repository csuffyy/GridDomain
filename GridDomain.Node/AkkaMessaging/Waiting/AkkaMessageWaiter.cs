using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging.Akka;
using GridDomain.Node.Actors;
using MemBus.Support;

namespace GridDomain.Node.AkkaMessaging.Waiting
{
    abstract class AdvancedMessageWaiter : IMessageWaiter
    {
        private readonly IActorRef _waiter;
        private readonly TimeSpan _timeout;

        public AdvancedMessageWaiter(IActorRef waiter, TimeSpan timeout)
        {
            _timeout = timeout;
            _waiter = waiter;
        }

        public Task<IWaitResults> ReceiveAll()
        {
            return _waiter.Ask<ExpectedMessagesReceived>(NotifyOnWaitEnd.Instance, _timeout)
                          .ContinueWith(t => (IWaitResults)new WaitResults(t.Result.Received), 
                                      TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public Task<TMsg> Receive<TMsg>(Predicate<TMsg> filter = null)
        {
            return _waiter.Ask<TMsg>(new NotifyOnMessage(typeof(TMsg)), _timeout)
                          .ContinueWith(t => filter?.Invoke(t.Result) == true ?
                                            t.Result : Receive(filter).Result, 
                             TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }


    //class AkkaMessageLocalWaiter// : IMessageWaiter, IDisposable
    //{
    //    private readonly IActorSubscriber _subscriber;
    //    private readonly TimeSpan _defaultTimeout;
    //    private readonly ConcurrentBag<object> _allMessages = new ConcurrentBag<object>();
    //    private readonly IDictionary<Type, Predicate<object>> _filters = new Dictionary<Type, Predicate<object>>();

    //    public AkkaMessageLocalWaiter(ActorSystem system, IActorSubscriber subscriber, TimeSpan defaultTimeout)
    //    {
    //        _defaultTimeout = defaultTimeout;
    //        _subscriber = subscriber;
    //    }

    //    public void Subscribe<TMsg>(Predicate<TMsg> filter = null)
    //    {
    //        _filters[typeof(TMsg)] = o => (filter ?? (t => true))((TMsg)o);

    //    }

    //    protected virtual bool RememberMessage(object msg)
    //    {
    //        if (!IsMessageExpected(msg)) return false;
    //        _allMessages.Add(msg);
    //        return true;
    //    }

    //    protected virtual bool IsWaitEnded(object lastMessage)
    //    {
    //        return IsFault(lastMessage) || AllExpectedMessagesReceived();
    //    }

    //    private bool AllExpectedMessagesReceived()
    //    {
    //        return _filters.Values.All(f => _allMessages.Any(m => f(m)));
    //    }

    //    private bool IsFault(object lastMessage)
    //    {
    //        if (!(lastMessage is Failure) && !(lastMessage is IFault) && !(lastMessage is Status.Failure)) return false;
    //        _allMessages.Add(lastMessage);
    //        return true;
    //    }

    //    protected virtual bool IsMessageExpected(object msg)
    //    {
    //        Predicate<object> filter;
    //        return _filters.TryGetValue(msg.GetType(), out filter) && filter(msg);
    //    }

    //    private void Start()
    //    {
    //        _inbox.ReceiveAsync(_defaultTimeout)
    //              .ContinueWith(t =>
    //              {
    //                  OnMessageReceived(this, t.Result);
    //                  if (!IsWaitEnded(t)) Start();
    //                  WaitEnd.SetResult(true);
    //                  WaitComplete(this, null);
    //              }, TaskContinuationOptions.OnlyOnRanToCompletion);
    //    }

    //    public event EventHandler<object> OnMessageReceived = delegate { };
    //    public event EventHandler WaitComplete = delegate { };

    //    protected readonly TaskCompletionSource<bool> WaitEnd = new TaskCompletionSource<bool>();

    //    public IReadOnlyCollection<object> AllReceivedMessages => _allMessages;

    //    public Task WaitAll()
    //    {
    //        Start();
    //        return WaitEnd.Task;
    //    }

    //    public TMsg Received<TMsg>(Predicate<TMsg> selector = null)
    //    {
    //        var sel = selector ?? (m => true);
    //        return _allMessages.OfType<TMsg>().FirstOrDefault(i => sel(i));
    //    }

    //    public void Dispose()
    //    {
    //        _inbox.Dispose();
    //    }

    //    public Task<IWaitResults> ReceiveAll()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}