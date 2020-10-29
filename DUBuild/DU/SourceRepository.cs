using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DUBuild.DU
{
    public class SourceRepository
    {
        //Classname => SourceFile
        private Dictionary<string, SourceFile> sources;

        public SourceRepository()
        {
            sources = new Dictionary<string, SourceFile>();
        }

        public void Add(SourceFile source)
        {
            if (this.sources.ContainsKey(source.ClassName))
            {
                var existing = sources[source.ClassName];
                if (existing == source)
                {
                    //attempting to add a duplicate source file
                    return;
                }
                else
                {
                    //Duplicate classname
                    throw new Exception($"Attempt to add a duplicate class {source.ClassName}, this class exists in {source.File.Name} and {existing.File.Name}");
                }
            }
            this.sources.Add(source.ClassName, source);
        }

        public SourceFile GetByClassname(string className)
        {
            return sources[className] ?? null;
        }
        public SourceFile GetByFilename(string filename)
        {
            return sources.Values.Where(x => x.File.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public bool ClassExists(string className)
        {
            return sources.ContainsKey(className);
        }
    }
}
