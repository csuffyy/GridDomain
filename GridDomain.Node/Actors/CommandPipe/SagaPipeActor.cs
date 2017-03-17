using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Util.Internal;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.Node.Actors.CommandPipe.ProcessorCatalogs;

namespace GridDomain.Node.Actors.CommandPipe
{
    /// <summary>
    ///     Synhronize sagas processing for produced domain events
    ///     If message process policy is set to synchronized, will process such events one after one
    ///     Will process all other messages in parallel
    /// </summary>
    public class SagaPipeActor : ReceiveActor
    {
        public const string SagaProcessActorRegistrationName = nameof(SagaProcessActorRegistrationName);
        private readonly IProcessorListCatalog _catalog;
        private IActorRef _commandExecutionActor;

        public SagaPipeActor(IProcessorListCatalog catalog)
        {
            _catalog = catalog;

            Receive<Initialize>(i =>
                                {
                                    _commandExecutionActor = i.CommandExecutorActor;
                                    Sender.Tell(Initialized.Instance);
                                });
            //part of events or fault from command execution
            ReceiveAsync<IMessageMetadataEnvelop>(env => ProcessSagas(env).PipeTo(Self));
            Receive<IEnumerable<MessageMetadataEnvelop<ICommand>>>(commandEnvelops =>
                                                                   {
                                                                       foreach (var envelop in commandEnvelops)
                                                                           _commandExecutionActor.Tell(envelop);
                                                                   });

            Receive<SagasProcessComplete>(m => SendCommandForExecution(m));
        }

        private void SendCommandForExecution(SagasProcessComplete m)
        {
            foreach (var command in m.ProducedCommands)
                _commandExecutionActor.Tell(new MessageMetadataEnvelop<ICommand>(command, m.Metadata.CreateChild(command.Id)));
        }

        private Task<IEnumerable<MessageMetadataEnvelop<ICommand>>> ProcessSagas(IMessageMetadataEnvelop messageMetadataEnvelop)
        {
            var eventProcessors = _catalog.Get(messageMetadataEnvelop.Message);
            if (!eventProcessors.Any())
                return Task.FromResult(Enumerable.Empty<MessageMetadataEnvelop<ICommand>>());

            return
                Task.WhenAll(eventProcessors.Select(e => e.ActorRef.Ask<ISagaTransitCompleted>(messageMetadataEnvelop)))
                    .ContinueWith(t => CreateCommandEnvelops(t.Result.OfType<SagaTransited>()));
        }

        private static IEnumerable<MessageMetadataEnvelop<ICommand>> CreateCommandEnvelops(IEnumerable<SagaTransited> messages)
        {
            return
                messages.SelectMany(msg => msg.ProducedCommands
                                              .Select(c => new MessageMetadataEnvelop<ICommand>(c,
                                                                                                msg.Metadata.CreateChild(c.Id, msg.SagaProcessEntry))));
        }
    }
}