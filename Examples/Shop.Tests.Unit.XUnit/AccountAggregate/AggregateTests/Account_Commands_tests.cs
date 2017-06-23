using System;
using System.Collections.Generic;
using GridDomain.EventSourcing;
using GridDomain.Tests.Common;
using NMoneys;
using Ploeh.AutoFixture;
using Shop.Domain.Aggregates.AccountAggregate;
using Shop.Domain.Aggregates.AccountAggregate.Commands;
using Shop.Domain.Aggregates.AccountAggregate.Events;
using Xunit;

namespace Shop.Tests.Unit.XUnit.AccountAggregate.AggregateTests
{
    public class Account_Commands_tests
    {
        [Fact]
        public void When_account_replenished_Then_amount_should_be_increased()
        {
            ReplenishAccountByCardCommand command;
            Money initialAmount;
            var scenario = new AggregateScenario<Account, AccountCommandsHandler>();

            scenario.Given(new AccountCreated(scenario.Id, Guid.NewGuid(), 123),
                           new AccountReplenish(scenario.Id, Guid.NewGuid(), initialAmount = new Money(100)))
                     .When(command = new ReplenishAccountByCardCommand(scenario.Id, new Money(12), "xxx123"))
                     .Then(new AccountReplenish(scenario.Id, command.Id, command.Amount))
                     .Run();

            Assert.Equal(initialAmount + command.Amount, scenario.Aggregate.Amount);
        }

        [Fact]
        public void When_account_withdrawal_Then_amount_should_be_decreased()
        {
            ReplenishAccountByCardCommand command;
            Money initialAmount;
            var scenario = new AggregateScenario<Account, AccountCommandsHandler>();

            scenario.Given(new AccountCreated(scenario.Id, Guid.NewGuid(), 123),
                           new AccountReplenish(scenario.Id, Guid.NewGuid(), initialAmount = new Money(100)))
                     .When(command = new ReplenishAccountByCardCommand(scenario.Id, new Money(12), "xxx123"))
                     .Then(new AccountReplenish(scenario.Id, command.Id, command.Amount))
                     .Run();

            Assert.Equal(initialAmount + command.Amount, scenario.Aggregate.Amount);
        }


    }
}