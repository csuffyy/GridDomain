﻿using System;
using System.Linq;
using GridDomain.Common;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Adapters;
using GridDomain.Tests.Framework.Configuration;
using GridDomain.Tools.Repositories;
using GridDomain.Tools.Repositories.EventRepositories;
using NUnit.Framework;

namespace GridDomain.Tests.Unit.Tools.Repositories
{

    [TestFixture]
    [Ignore("Not working for now")]
    public class Test_event_repositoty_tools
    {
        private class Message :DomainEvent
        {
            public int Id;

            public Message(Guid sourceId, DateTime? createdTime = null, Guid sagaId = new Guid()) : base(sourceId, createdTime, sagaId)
            {
            }
        }

        private class Message2 : Message
        {
            public Message2(Guid sourceId, DateTime? createdTime = null, Guid sagaId = new Guid()) : base(sourceId, createdTime, sagaId)
            {
            }
        }

        [Test]
        public void Given_events_When_save_by_events_repository_Then_events_can_be_fetched()
        {
            var events = new DomainEvent[]
            {
                new Message(Guid.NewGuid())  {Id = 1}, 
                new Message2(Guid.NewGuid()) {Id = 2}
            };

           var persistId = "testId";
            var saveTime = BusinessDateTime.UtcNow;
           var repo = CreateRepository();
           repo.Save(persistId, events);

           var eventsLoaded = repo.Load(persistId).Where(e => e.CreatedTime >= saveTime).Cast<Message>();
           CollectionAssert.AreEquivalent(events.Cast<Message>().Select(e => e.Id),eventsLoaded.Select(e=> e.Id));
        }

        protected virtual IRepository<DomainEvent> CreateRepository()
        {
            return ActorSystemEventRepository.New(new AutoTestAkkaConfiguration(), new EventsAdaptersCatalog());
        }
    }
}
