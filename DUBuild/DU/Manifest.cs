using System;
using System.Collections.Generic;
using System.Text;

namespace DUBuild.DU
{
    public class Manifest
    {
        public string MainFile { get; set; }
        public string OutputFilename { get; set; }
        public bool Minify { get; set; }
        public IEnumerable<string> Resources { get; set; }

    }
}
