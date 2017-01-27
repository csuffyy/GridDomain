using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.TestKit.Xunit2;
using GridDomain.Common;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;
using GridDomain.Node;
using GridDomain.Node.Configuration.Akka.Hocon;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Scheduling.Quartz;
using GridDomain.Tests.Framework;
using GridDomain.Tests.XUnit.EventsUpgrade;
using GridDomain.Tests.XUnit.EventsUpgrade.Domain;
using GridDomain.Tests.XUnit.EventsUpgrade.Domain.Events;
using Microsoft.Practices.Unity;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.XUnit
{
   //public abstract class NodeCommandsTest : TestKit
   //{
   //    public NodeCommandsTest(ITestOutputHelper helper):base(new NodeTestFixture(CreateConfiguration(),CreateMap()))
   //    {
   //        
   //    }
   //    protected virtual void OnNodeCreated()
   //    {
   //       // GridNode.EventsAdaptersCatalog.Register(new BalanceChangedDomainEventAdapter1());
   //    }
   //
   //    protected abstract IContainerConfiguration CreateConfiguration();
   //
   //    protected abstract IMessageRouteMap CreateMap();
   //}

    public abstract class NodeTestKit : TestKit
    {
        private NodeTestFixture NodeTestFixture { get;}
        protected GridDomainNode Node => NodeTestFixture.Node;
        protected NodeTestKit(ITestOutputHelper output, NodeTestFixture fixture): base(fixture.GetConfig(), fixture.Name)
        {
            Serilog.Log.Logger = new XUnitAutoTestLoggerConfiguration(output).CreateLogger();
            NodeTestFixture = fixture;
            NodeTestFixture.System = Sys;
        }

        //do not kill Akka system on each test run
        protected override void AfterAll()
        {
        }
    }
}