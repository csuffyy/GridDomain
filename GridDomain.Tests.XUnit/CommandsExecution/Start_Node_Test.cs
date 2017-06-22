using System;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Scheduling;
using GridDomain.Scheduling.Integration;
using GridDomain.Scheduling.Quartz;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.XUnit.CommandsExecution {
    public class Start_Node_Test : SampleDomainCommandExecutionTests
    {
        private string  _short_config = @"akka {
                actor {
                    serialize-creators = on
                    serializers {
                        hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                    }

                    serialization-bindings {
                        ""System.Object"" = hyperion
     
                    }
                 }

             actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                remote {
                    helios.tcp {
                        transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
                        transport-protocol = tcp
                        hostname = 127.0.0.1
                    }
                }
            }";

        const string config = @"akka {

                stdout-loglevel = DEBUG
                loglevel=DEBUG
                loggers=[""GridDomain.Tests.Framework.LoggerActorDummy, GridDomain.Tests.Framework""]

            actor.debug {#autoreceive = on
#lifecycle = on
#receive = on
#router-misconfiguration = on
#event-stream = on 
                unhandled = on
            }

            actor {
# serialize-messages = on
                serialize-creators = on
                serializers {
                    wire = ""Akka.Serialization.WireSerializer, Akka.Serialization.Wire""
                    json = ""GridDomain.Node.DomainEventsJsonAkkaSerializer, GridDomain.Node""
                    hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                }

                serialization-bindings {
                    ""GridDomain.EventSourcing.DomainEvent, GridDomain.EventSourcing"" = json
                    ""GridDomain.EventSourcing.CommonDomain.IMemento, GridDomain.EventSourcing"" = json
# for local snapshots storage
                    ""Akka.Persistence.Serialization.Snapshot, Akka.Persistence"" = json
                    ""System.Object"" = hyperion
     
                }
            }

            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote {
                log - remote - lifecycle - events = DEBUG
                helios.tcp {
                    transport -class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
                    transport-protocol = tcp
                    port = 0
                    hostname =  127.0.0.1
        public-hostname = 127.0.0.1
        enforce-ip-family = true
    }
}
persistence {
publish-plugin-commands = on

    journal {
    plugin = ""akka.persistence.journal.inmem""
    inmem {
        class = ""Akka.Persistence.Journal.MemoryJournal, Akka.Persistence""
        plugin-dispatcher = ""akka.actor.default-dispatcher""
                            
            event-adapters {
            upd = ""GridDomain.Node.AkkaDomainEventsAdapter, GridDomain.Node""
        }
        event-adapter-bindings {
            ""GridDomain.EventSourcing.DomainEvent, GridDomain.EventSourcing"" = upd
        }
    }
}

 
snapshot-store {
    plugin = ""akka.persistence.snapshot-store.local""
    local {
        class = ""Akka.Persistence.Snapshot.LocalSnapshotStore, Akka.Persistence""
        plugin-dispatcher = ""akka.persistence.dispatchers.default-plugin-dispatcher""
        stream-dispatcher = ""akka.persistence.dispatchers.default-stream-dispatcher""
        dir = LocalSnapshots
    }
}
}
}";
        public Start_Node_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Create_Node()
        {
            var a = Node.System;
        }

    }
}