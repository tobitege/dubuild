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
        private static Regex regex_timers = new Regex(@"[ \t]*--@timer[ \t]+(\S+)", RegexOptions.Compiled);
        private static Regex regex_keybinds = new Regex(@"[ \t]*--@keybind[ \t]+(\S+)[ \t]+(true|false)[ \t]+([\w ]+)", RegexOptions.Compiled);

        public class NoClassNameException : Exception
        {
            public NoClassNameException(string message)
                : base(message) { }
        }

        public class Keybind
        {
            public string KeybindName { get; set; }
            public bool Toggle { get; set; }
            public string Description { get; set; }

            public static Keybind FromRegex(string name, string toggle, string description)
            {
                if (Boolean.TryParse(toggle, out var toggleBool))
                {
                    return new Keybind() { KeybindName = name, Description = description, Toggle = toggleBool };
                }
                return null;
            }
        }

        public System.IO.FileInfo File { get; set; }
        public string ClassName { get; set; }
        public IEnumerable<string> Dependencies { get; set; }
        public string GitHash { get; set; }
        public string Contents { get; set; }
        public string OutFilename { get; set; }
        public IEnumerable<string> Timers { get; set; }
        public IEnumerable<Keybind> Keybinds { get; set; }

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

            var timers_match = regex_timers.Matches(sf.Contents);
            if (timers_match.Count > 0)
            {
                sf.Timers = timers_match.Select(x => x.Groups[1].Value);
            }

            var keybinds_match = regex_keybinds.Matches(sf.Contents);
            if (keybinds_match.Count > 0)
            {
                sf.Keybinds = keybinds_match.Select(x => Keybind.FromRegex(x.Groups[1].Value, x.Groups[2].Value, x.Groups[3].Value)).Where(x=>x != null);
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
