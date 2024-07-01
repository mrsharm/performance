using FluentAssertions;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers.GCDynamic;
using GC.Analysis.API.DynamicEvents;
using System.Reflection;

namespace GC.Analysis.API.UnitTests
{
    [TestClass]
    public class DynamicEventTests
    {
        [TestMethod]
        public void TestDuplicatedSchema()
        {
            Action test = () =>
            {
                DynamicEventSchema.Set(
                    new List<DynamicEventSchema>
                    {
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(ushort)),
                                new KeyValuePair<string, Type>("Number", typeof(ulong)),
                            }
                        },
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(ushort)),
                                new KeyValuePair<string, Type>("Number", typeof(ulong)),
                            }
                        }
                    }
                );
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestDuplicatedFields()
        {
            Action test = () =>
            {
                DynamicEventSchema.Set(
                    new List<DynamicEventSchema>
                    {
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(ushort)),
                                new KeyValuePair<string, Type>("version", typeof(ulong)),
                            }
                        },
                    }
                );
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestUnsupportedType()
        {
            Action test = () =>
            {
                DynamicEventSchema.Set(
                    new List<DynamicEventSchema>
                    {
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(DateTime)),
                            }
                        },
                    }
                );
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestNegativeMinOccurrence()
        {
            Action test = () =>
            {
                DynamicEventSchema.Set(
                    new List<DynamicEventSchema>
                    {
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            MinOccurrence = -1,
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(ushort)),
                            }
                        },
                    }
                );
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestSmallerMaxOccurrence()
        {
            Action test = () =>
            {
                DynamicEventSchema.Set(
                    new List<DynamicEventSchema>
                    {
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            MaxOccurrence = 0,
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(ushort)),
                            }
                        },
                    }
                );
            };
            test.Should().Throw<Exception>();
        }

        private List<DynamicEventSchema> correctSingleSchema = new List<DynamicEventSchema>
        {
            new DynamicEventSchema
            {
                DynamicEventName = "SampleEventName",
                Fields = new List<KeyValuePair<string, Type>>
                {
                    new KeyValuePair<string, Type>("version", typeof(ushort)),
                    new KeyValuePair<string, Type>("Number", typeof(ulong)),
                }
            },
        };

        private DynamicEvent sampleEvent = new DynamicEvent(
            "SampleEventName",
            DateTime.Now,
            new byte[] { 1, 0, 2, 0, 0, 0, 0, 0, 0, 0 }
        );

        [TestMethod]
        public void TestMissedSingleEvent()
        {
            DynamicEventSchema.Set(correctSingleSchema);
            List<DynamicEvent> dynamicEvents = new List<DynamicEvent>();
            Action test = () =>
            {
                dynamic index = new DynamicIndex(dynamicEvents);
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestDuplicatedSingleEvent()
        {
            DynamicEventSchema.Set(correctSingleSchema);
            List<DynamicEvent> dynamicEvents = new List<DynamicEvent>
            {
                sampleEvent,
                sampleEvent
            };
            Action test = () =>
            {
                dynamic index = new DynamicIndex(dynamicEvents);
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestSingleEvent()
        {
            DynamicEventSchema.Set(correctSingleSchema);
            List<DynamicEvent> dynamicEvents = new List<DynamicEvent>
            {
                sampleEvent
            };
            dynamic index = new DynamicIndex(dynamicEvents);

            ((int)index.SampleEventName.version).Should().Be(1);
            ((int)index.SampleEventName.Number).Should().Be(2);
        }

        private List<DynamicEventSchema> correctMultipleSchema = new List<DynamicEventSchema>
        {
            new DynamicEventSchema
            {
                DynamicEventName = "SampleEventName",
                MaxOccurrence = 2,
                Fields = new List<KeyValuePair<string, Type>>
                {
                    new KeyValuePair<string, Type>("version", typeof(ushort)),
                    new KeyValuePair<string, Type>("Number", typeof(ulong)),
                }
            },
        };

        [TestMethod]
        public void TestMissedMultipleEvent()
        {
            DynamicEventSchema.Set(correctMultipleSchema);
            List<DynamicEvent> dynamicEvents = new List<DynamicEvent>();
            Action test = () =>
            {
                dynamic index = new DynamicIndex(dynamicEvents);
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestTooManyMultipleEvents()
        {
            DynamicEventSchema.Set(correctMultipleSchema);
            List<DynamicEvent> dynamicEvents = new List<DynamicEvent>
            {
                sampleEvent,
                sampleEvent,
                sampleEvent,
            };
            Action test = () =>
            {
                dynamic index = new DynamicIndex(dynamicEvents);
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestMultipleEvents()
        {
            DynamicEventSchema.Set(correctMultipleSchema);
            List<DynamicEvent> dynamicEvents = new List<DynamicEvent>
            {
                sampleEvent,
                sampleEvent,
            };
            dynamic index = new DynamicIndex(dynamicEvents);

            ((int)index.SampleEventName[0].version).Should().Be(1);
            ((int)index.SampleEventName[0].Number).Should().Be(2);
            ((int)index.SampleEventName[1].version).Should().Be(1);
            ((int)index.SampleEventName[1].Number).Should().Be(2);
        }

        [TestMethod]
        public void TestOptionalEvent()
        {
            DynamicEventSchema.Set(
                new List<DynamicEventSchema>
                {
                    new DynamicEventSchema
                    {
                        DynamicEventName = "SampleEventName",
                        MinOccurrence = 0,
                        Fields = new List<KeyValuePair<string, Type>>
                        {
                            new KeyValuePair<string, Type>("version", typeof(ushort)),
                            new KeyValuePair<string, Type>("Number", typeof(ulong)),
                        }
                    },
                }
            );
            List<DynamicEvent> dynamicEvents = new List<DynamicEvent>();
            dynamic index = new DynamicIndex(dynamicEvents);
            (index.SampleEventName == null ? 1 : 0).Should().Be(1);
        }
    }
}