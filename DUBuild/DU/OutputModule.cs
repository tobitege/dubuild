using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DUBuild.DU
{
    public class OutputHandlerFilter
    {
        [JsonProperty("args")]
        public IEnumerable<Dictionary<string, string>> Args { get; set; }
        [JsonProperty("signature")]
        public string Signature { get; set; }
        [JsonProperty("slotKey")]
        public string SlotKey { get; set; }
    }
    public class OutputHandler
    {
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("filter")]
        public OutputHandlerFilter Filter { get; set; }
        [JsonProperty("key")]
        public string Key { get; set; }
    }
    public class OutputSlotType {
        [JsonProperty("events")]
        public IEnumerable<string> Events { get; set; }
        [JsonProperty("methods")]
        public IEnumerable<string> Methods { get; set; }

        public OutputSlotType()
        {
            Events = new List<string>();
            Methods = new List<string>();
        }
    }
    public class OutputSlot
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type")]
        public OutputSlotType Type { get; set; }
        
        public OutputSlot(int index)
        {
            Name = $"slot{index+1}";
            Type = new OutputSlotType();
        }
        public OutputSlot(string name)
            : this(0)
        {
            Name = name;
        }

    }
    public class OutputModule
    {
        public class SlotKey
        {
            public static SlotKey Library = new SlotKey(-3);
            public static SlotKey System = new SlotKey(-2);
            public static SlotKey Unit = new SlotKey(-1);

            public int Key { get; private set; }
            public SlotKey(int val)
            {
                this.Key = val;
            }

            public static implicit operator int(SlotKey key) =>key.Key;
            public static implicit operator string(SlotKey key) =>$"{key.Key}";
        }

        [JsonProperty("slots")]
        public Dictionary<int, OutputSlot> Slots { get; set; }
        [JsonProperty("handlers")]
        public IList<OutputHandler> Handlers { get; set; }
        [JsonProperty("methods")]
        public IList<string> Methods { get; set;}
        [JsonProperty("events")]
        public IList<string> Events { get; set; }

        public OutputModule()
        {
            Slots = new Dictionary<int, OutputSlot>();
            Handlers = new List<OutputHandler>();
            Methods = new List<string>();
            Events = new List<string>();
            for(int i=0; i<=20; i++)
            {
                Slots.Add(i, new OutputSlot(i));
            }
            Slots.Add(SlotKey.Library, new OutputSlot("library"));
            Slots.Add(SlotKey.System, new OutputSlot("system"));
            Slots.Add(SlotKey.Unit, new OutputSlot("unit"));
        }
    }
}
