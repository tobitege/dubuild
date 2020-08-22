using System;
using System.Collections.Generic;
using System.Text;

namespace DUBuild.DU
{
    public class OutputHandlerFilter
    {
        public IEnumerable<Dictionary<string, string>> Args { get; set; }
        public string Signature { get; set; }
        public string SlotKey { get; set; }
    }
    public class OutputHandler
    {
        public string Code { get; set; }
        public OutputHandlerFilter Filter { get; set; }
        public string Key { get; set; }
    }
    public class OutputSlotType {
        public IEnumerable<string> Events { get; set; }
        public IEnumerable<string> Methods { get; set; }

        public OutputSlotType()
        {
            Events = new List<string>();
            Methods = new List<string>();
        }
    }
    public class OutputSlot
    {
        public string Name { get; set; }
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
        public Dictionary<int, OutputSlot> Slots { get; set; }
        public IList<OutputHandler> Handlers { get; set; }
        public IList<string> Methods { get; set;}
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
            Slots.Add(-3, new OutputSlot("library"));
            Slots.Add(-2, new OutputSlot("system"));
            Slots.Add(-1, new OutputSlot("unit"));
        }
    }
}
