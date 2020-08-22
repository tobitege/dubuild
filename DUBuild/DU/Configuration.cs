using System;
using System.Collections.Generic;
using System.Text;

namespace DUBuild.DU
{
    public class CodeSlot
    {
        public IEnumerable<string> Files { get; set; }
        public string Code { get; set; }
        public string Slot { get; set; }
        public string Signature { get; set; }
        public string Args { get; set; }

    }
    public class Configuration
    {
        public bool Minify { get; set; }
        public bool Encrypt { get; set; }
        public IEnumerable<CodeSlot> Slots { get; set; }
    }
}
