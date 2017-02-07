﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging.Akka;
using GridDomain.Node.AkkaMessaging.Waiting;
using Xunit;

namespace GridDomain.Tests.XUnit.MessageWaiting
{
    public abstract class AkkaWaiterTest : IDisposable
    {
        private LocalAkkaEventBusTransport _transport;
        private ActorSystem _actorSystem;
        private Task<IWaitResults> _results;

        //Configure()
        protected AkkaWaiterTest()
        {
            _actorSystem = ActorSystem.Create("test");
            _transport = new LocalAkkaEventBusTransport(_actorSystem);
            Waiter = new AkkaMessageLocalWaiter(_actorSystem, _transport, TimeSpan.FromSeconds(1));
            _results = ConfigureWaiter(Waiter);
        }

        protected abstract Task<IWaitResults> ConfigureWaiter(AkkaMessageLocalWaiter waiter);

        public void Dispose()
        {
            _actorSystem.Terminate()
                        .Wait();
        }

        protected AkkaMessageLocalWaiter Waiter { get; }

        protected void Publish(params object[] messages)
        {
            foreach (var msg in messages)
                _transport.Publish(msg);
        }

        protected async Task ExpectMsg<T>(T msg, Predicate<T> filter = null, TimeSpan? timeout = null)
        {
            await _results;

            filter = filter ?? (t => true);
            Assert.Equal(msg,
                _results.Result.All.OfType<IMessageMetadataEnvelop>()
                        .Select(m => m.Message)
                        .OfType<T>()
                        .FirstOrDefault(t => filter(t)));
        }

        protected void ExpectNoMsg<T>(T msg, TimeSpan? timeout = null) where T : class
        {
            if (!_results.Wait(timeout ?? DefaultTimeout))
                return;

            var e = Assert.ThrowsAsync<TimeoutException>(() => ExpectMsg(msg));
        }

        public TimeSpan DefaultTimeout { get; } = TimeSpan.FromMilliseconds(50);

        protected void ExpectNoMsg()
        {
            ExpectNoMsg<object>(null);
        }
    }
}