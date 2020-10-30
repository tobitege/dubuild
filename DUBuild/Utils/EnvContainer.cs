using System;
using System.Collections.Generic;
using System.Text;

namespace DUBuild.Utils
{
    public class EnvContainer : Dictionary<string, string>
    {
        public EnvContainer()
        {
            var env = Environment.GetEnvironmentVariables();
            foreach (var e in env.Keys)
            {
                var key = e as string;
                this.Add(key, env[key] as string);
            }

            //Handle special keys here
            if (this.ContainsKey("CI_PIPELINE_IID") && !this.ContainsKey("CI_COMMIT_TAG")) this.Add("CI_COMMIT_TAG", $"Build {env["CI_PIPELINE_IID"] as string}");
        }
    }
}
