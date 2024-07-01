// TODO, andrewau, remove this condition when new TraceEvent is available through Nuget.
#if CUSTOM_TRACE_EVENT

using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers.GCDynamic;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GC.Analysis.API.UnitTests")]

namespace GC.Analysis.API.DynamicEvents
{
    public static class TraceGCExtensions
    {
        public static dynamic DynamicEvents(this TraceGC traceGC)
        {
            return new DynamicIndex(traceGC.DynamicEvents);
        }
    }

    public class DynamicEventSchema
    {
        internal static Dictionary<string, CompiledSchema> DynamicEventSchemas = new Dictionary<string, CompiledSchema>();

        public string DynamicEventName { get; set; }

        public List<KeyValuePair<string, Type>> Fields { get; set; }

        public int MinOccurrence { get; set; } = 1;

        public int MaxOccurrence { get; set; } = 1;

        public static void Set(List<DynamicEventSchema> dynamicEventSchemas)
        {
            DynamicEventSchemas.Clear();
            foreach (DynamicEventSchema dynamicEventSchema in dynamicEventSchemas)
            {
                if (DynamicEventSchemas.ContainsKey(dynamicEventSchema.DynamicEventName))
                {
                    throw new Exception($"Provided schema has a duplicated event named {dynamicEventSchema.DynamicEventName}");
                }
                CompiledSchema schema = new CompiledSchema();
                if (dynamicEventSchema.MinOccurrence < 0)
                {
                    throw new Exception($"Provided event named {dynamicEventSchema.DynamicEventName} has a negative MinOccurrence");
                }
                if (dynamicEventSchema.MaxOccurrence < dynamicEventSchema.MinOccurrence)
                {
                    throw new Exception($"Provided event named {dynamicEventSchema.DynamicEventName} has a MaxOccurrence smaller than MinOccurrence");
                }
                schema.MinOccurrence = dynamicEventSchema.MinOccurrence;
                schema.MaxOccurrence = dynamicEventSchema.MaxOccurrence;
                int offset = 0;
                foreach (KeyValuePair<string, Type> field in dynamicEventSchema.Fields)
                {
                    if (schema.ContainsKey(field.Key))
                    {
                        DynamicEventSchemas.Clear();
                        throw new Exception($"Provided event named {dynamicEventSchema.DynamicEventName} has a duplicated field named {field.Key}");
                    }
                    schema.Add(field.Key, new DynamicEventField { FieldOffset = offset, FieldType = field.Value });

                    if (field.Value == typeof(ushort))
                    {
                        offset += 2;
                    }
                    else if (field.Value == typeof(uint))
                    {
                        offset += 4;
                    }
                    else if (field.Value == typeof(float))
                    {
                        offset += 4;
                    }
                    else if (field.Value == typeof(ulong))
                    {
                        offset += 8;
                    }
                    else
                    {
                        DynamicEventSchemas.Clear();
                        throw new Exception($"Provided event named {dynamicEventSchema.DynamicEventName} has a field named {field.Key} using an unsupported type {field.Value}");
                    }
                }
                schema.Size = offset;
                DynamicEventSchemas.Add(dynamicEventSchema.DynamicEventName, schema);
            }
        }
    }

    internal class DynamicEventField
    {
        public int FieldOffset { get; set; }
        public Type FieldType { get; set; }
    }

    internal class CompiledSchema : Dictionary<string, DynamicEventField>
    {
        public int MinOccurrence { get; set; }
        public int MaxOccurrence { get; set; }
        public int Size { get; set; }
    }

    internal class DynamicIndex : DynamicObject
    {
        private Dictionary<string, object> newIndex;

        public DynamicIndex(List<DynamicEvent> dynamicEvents)
        {
            this.newIndex = new Dictionary<string, object>();
            Dictionary<string, List<DynamicEvent>> indexedEvents = new Dictionary<string, List<DynamicEvent>>();
            foreach (string eventName in DynamicEventSchema.DynamicEventSchemas.Keys)
            {
                indexedEvents.Add(eventName, new List<DynamicEvent>());
            }
            foreach (DynamicEvent dynamicEvent in dynamicEvents)
            {
                List<DynamicEvent> dynamicEventList;
                if (indexedEvents.TryGetValue(dynamicEvent.Name, out dynamicEventList))
                {
                    dynamicEventList.Add(dynamicEvent);
                }
                else
                {
                    this.newIndex = null;
                    throw new Exception();
                }
            }
            foreach (string eventName in DynamicEventSchema.DynamicEventSchemas.Keys)
            {
                List<DynamicEvent> eventList = indexedEvents[eventName];
                CompiledSchema schema = DynamicEventSchema.DynamicEventSchemas[eventName];
                if (eventList.Count > schema.MaxOccurrence)
                {
                    this.newIndex = null;
                    throw new Exception($"More than {schema.MaxOccurrence} {eventName} is found.");
                }
                if (eventList.Count < schema.MinOccurrence)
                {
                    this.newIndex = null;
                    throw new Exception();
                }
                if (schema.MaxOccurrence == 1)
                {
                    if (eventList.Count >= 1)
                    {
                        this.newIndex.Add(eventName, new DynamicEventObject(eventList[0], schema));
                    }
                    else
                    {
                        this.newIndex.Add(eventName, null);
                    }
                }
                else
                {
                    List<DynamicEventObject> output = new List<DynamicEventObject>();
                    foreach (DynamicEvent dynamicEvent in eventList)
                    {
                        output.Add(new DynamicEventObject(dynamicEvent, schema));
                    }
                    this.newIndex.Add(eventName, output);
                }
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string name = binder.Name;
            CompiledSchema schema;
            if (DynamicEventSchema.DynamicEventSchemas.TryGetValue(name, out schema))
            {
                result = newIndex[name];
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }

    internal class DynamicEventObject : DynamicObject
    {
        private DynamicEvent dynamicEvent;
        private Dictionary<string, object> fieldValues;

        public DynamicEventObject(DynamicEvent dynamicEvent, CompiledSchema schema)
        {
            this.dynamicEvent = dynamicEvent;
            this.fieldValues = new Dictionary<string, object>();
            if (dynamicEvent.Payload.Length != schema.Size)
            {
                throw new Exception($"Event {dynamicEvent.Name} does not have matching size");
            }
            foreach (KeyValuePair<string, DynamicEventField> field in schema)
            {
                object value = null;
                int fieldOffset = field.Value.FieldOffset;
                Type fieldType = field.Value.FieldType;

                if (fieldType == typeof(ushort))
                {
                    value = BitConverter.ToUInt16(dynamicEvent.Payload, fieldOffset);
                }
                else if (fieldType == typeof(uint))
                {
                    value = BitConverter.ToUInt32(dynamicEvent.Payload, fieldOffset);
                }
                else if (fieldType == typeof(float))
                {
                    value = BitConverter.ToSingle(dynamicEvent.Payload, fieldOffset);
                }
                else if (fieldType == typeof(ulong))
                {
                    value = BitConverter.ToUInt64(dynamicEvent.Payload, fieldOffset);
                }
                else
                {
                    throw new Exception($"Provided schema has a field named {field.Key} using an unsupported type {fieldType}");
                }
                this.fieldValues.Add(field.Key, value);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string name = binder.Name;
            if (string.Equals(name, "TimeStamp"))
            {
                result = this.dynamicEvent.TimeStamp;
                return true;
            }
            else
            if (this.fieldValues.TryGetValue(name, out var fieldValue))
            {
                result = fieldValue;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public override string ToString()
        {
            return "I am " + this.dynamicEvent.Name + " with these fields: \n" + string.Join("\n", this.fieldValues.Select(kvp => kvp.Key + "->" + kvp.Value));
        }
    }
}

#endif