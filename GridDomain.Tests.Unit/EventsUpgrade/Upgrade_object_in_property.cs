﻿using GridDomain.EventSourcing.Adapters;
using Newtonsoft.Json;
using NUnit.Framework;

namespace GridDomain.Tests.Unit.EventsUpgrade
{
    [TestFixture]
    class Upgrade_object_in_property
    {
        private interface ISubObject
        {
            string Value { get; set; }
        }

        class SubObject_V1 : ISubObject
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        class SubObject_V2 : ISubObject
        {
            public int number { get; set; }
            public string Value { get; set; }
        }

        class Event
        {
            public ISubObject Payload { get; set; }
        }

        class SubObjectConverter : ObjectAdapter<SubObject_V1, SubObject_V2>
        {
            public override SubObject_V2 Convert(SubObject_V1 value)
            {
                return new SubObject_V2() { number = int.Parse(value.Name), Value = value.Value };
            }
        }



        [Test]
        public void Property_upgraded_from_serialized_value()
        {
            var settings = DomainSerializer.GetDefaultSettings();
            settings.Converters.Add(new SubObjectConverter());

            var serializedValue = @"{
                              ""$id"": ""1"",
                              ""Payload"": {
                                ""$id"": ""2"",
                                ""$type"": """+ typeof(SubObject_V1).AssemblyQualifiedName + @""",
                                ""Name"": ""10"",
                                ""Value"": ""123""
                              }
                            }";

            var restoredEvent = JsonConvert.DeserializeObject<Event>(serializedValue, settings);
            Assert.IsInstanceOf<SubObject_V2>(restoredEvent.Payload);
        }


        [Test]
        public void Propert_is_upgraded()
        {
            var initialEvent = new Event() {Payload = new SubObject_V1() {Name = "10",Value = "123"} };

            var settings = DomainSerializer.GetDefaultSettings();
            settings.Converters.Add(new SubObjectConverter());
     
            var serializedValue = JsonConvert.SerializeObject(initialEvent, settings);
            var restoredEvent = JsonConvert.DeserializeObject<Event>(serializedValue, settings);

            Assert.IsInstanceOf<SubObject_V2>(restoredEvent.Payload);
        }
    }
}
