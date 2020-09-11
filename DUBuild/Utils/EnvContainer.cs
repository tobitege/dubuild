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
        }
    }
}
