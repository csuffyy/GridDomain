using System;
using GridDomain.Tests.Sagas.SoftwareProgrammingDomain.Events;
using NUnit.Framework;

namespace GridDomain.Tests.Sagas.InstanceSagas
{
    [TestFixture]
    class Given_saga_When_publishing_several_start_messages : Given_saga_When_publishing_any_of_start_messages
    {
        private static readonly Guid SagaId = Guid.NewGuid();
        private static  GotTiredEvent firstMessage;
        private static  SleptWellEvent secondMessage;
        private static object[] GetMessages(Guid sagaId)
        {
            firstMessage = new GotTiredEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), sagaId);
            secondMessage = new SleptWellEvent(Guid.NewGuid(), Guid.NewGuid(), sagaId);

            return new object[]
            {
                firstMessage,
                secondMessage
            };
        }

        public Given_saga_When_publishing_several_start_messages(): base(SagaId,GetMessages(SagaId))
        {

        }

        [Then]
        public void Saga_reinitialized_from_last_start_message()
        {
            Assert.AreEqual(secondMessage.PersonId, SagaData.Data.PersonId);
        }
    }
}