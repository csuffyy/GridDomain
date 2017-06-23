using System;
using System.Collections.Generic;
using GridDomain.EventSourcing;
using GridDomain.Tests.Common;
using Ploeh.AutoFixture;
using Shop.Domain.Aggregates.AccountAggregate;
using Shop.Domain.Aggregates.AccountAggregate.Commands;
using Shop.Domain.Aggregates.AccountAggregate.Events;
using Xunit;

namespace Shop.Tests.Unit.XUnit.AccountAggregate.AggregateTests
{
    /// <summary>
    /// Tests for Account aggregate creation from command. 
    /// Example for aggregate scenario usage
    /// </summary>
    public class Account_creation_tests
    {
        private readonly CreateAccountCommand _command;
        private readonly AggregateScenario<Account, AccountCommandsHandler> _scenario;

        public Account_creation_tests()
        {
            _command = new Fixture().Create<CreateAccountCommand>();

            _scenario = AggregateScenario<Account,AccountCommandsHandler>.New()
                                           .When(_command)
                                           .Then(new AccountCreated(_command.AccountId, _command.UserId, _command.Number))
                                           .Run();
        }

        [Fact]
        public void Aggregate_should_take_id_from_command()
        {
            Assert.Equal(_command.AccountId, _scenario.Aggregate.Id);
        }

        [Fact]
        public void Aggregate_should_take_number_from_command()
        {
            Assert.Equal(_command.Number, _scenario.Aggregate.Number);
        }

        [Fact]
        public void Aggregate_should_take_user_from_command()
        {
            Assert.Equal(_command.UserId, _scenario.Aggregate.UserId);
        }
    }
}