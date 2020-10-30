using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace DUBuild.DU
{
    public class SourceFile
    {
        private static Regex regex_classname = new Regex(@"[ \t]*--@class[ \t]+(\S+)", RegexOptions.Compiled);
        private static Regex regex_dependencies = new Regex(@"[ \t]*--@require[ \t]+(\S+)", RegexOptions.Compiled);
        private static Regex regex_outName = new Regex(@"[ \t]*--@outFilename[ \t]+(\S+)", RegexOptions.Compiled);

        public class NoClassNameException : Exception
        {
            public NoClassNameException(string message)
                : base(message) { }
        }

        public System.IO.FileInfo File { get; set; }
        public string ClassName { get; set; }
        public IEnumerable<string> Dependencies { get; set; }
        public string GitHash { get; set; }
        public string Contents { get; set; }
        public string OutFilename { get; set; }

        public SourceFile()
        {
            Dependencies = new List<string>();
        }

        public static SourceFile Parse(string path)
        {
            return Parse(new System.IO.FileInfo(path));
        }
        public static SourceFile Parse(System.IO.FileInfo fileinfo, Utils.GitContainer git)
        {
            var sf = Parse(fileinfo);
            var relativePath = git.ConvertToRelative(fileinfo.FullName);
            sf.GitHash = git.GetFileLastModifiedCommit(relativePath);
            return sf;
        }
        public static SourceFile Parse(System.IO.FileInfo fileinfo)
        {
            var sf = new SourceFile();
            sf.File = fileinfo;

            using (var sr = fileinfo.OpenText())
            {
                sf.Contents = sr.ReadToEnd();
            }

            var classname_match = regex_classname.Match(sf.Contents);
            if (!classname_match.Success) throw new NoClassNameException($"File {fileinfo.Name} does not have a @class attribute");
            sf.ClassName = classname_match.Groups[1].Value;

            var dependencies_match = regex_dependencies.Matches(sf.Contents);
            if (dependencies_match.Count > 0)
            {
                sf.Dependencies = dependencies_match.Select(x => x.Groups[1].Value);
            }

            var outFilename_match = regex_outName.Match(sf.Contents);
            if (outFilename_match.Success)
            {
                var fn = outFilename_match.Groups[1].Value;
                if (fn.Length > 1)
                {
                    sf.OutFilename = fn;
                }
            }

            sf.GitHash = "NOHASH";

            return sf;
        }
    }
}
