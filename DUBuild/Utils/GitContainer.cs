using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DUBuild.Utils
{
    public class GitContainer
    {
        private string gitPath;
        private bool Enabled;
        public GitContainer(string gitPath)
        {
            this.gitPath = gitPath;
            var ps = new ProcessStartInfo()
            {
                FileName = "git",
                Arguments = "version",
                WorkingDirectory = gitPath
            };

            var p = Process.Start(ps);
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                Enabled = false;
            }
            else
            {
                Enabled = true;
            }
        }

        public string GetFileLastModifiedCommit(string filePath)
        {
            if (!Enabled) return "Unknown";

            var ps = new ProcessStartInfo()
            {
                FileName = "git",
                Arguments = String.Format("rev-list -1 HEAD {0}", filePath),
                RedirectStandardOutput = true,
                WorkingDirectory = gitPath
            };
            var p = Process.Start(ps);
            p.WaitForExit();
            if (p.ExitCode != 0) return "Unknown";

            return p.StandardOutput.ReadToEnd().Replace("\n", String.Empty).Replace("\r", String.Empty);
        }

        public string ConvertToRelative(string absolutePath)
        {
            return this.ConvertToRelative(this.gitPath, absolutePath);
        }
        public string ConvertToRelative(string basePath, string absolutePath)
        {
            return absolutePath.Replace(basePath + "\\", String.Empty);
        }
    }
}
