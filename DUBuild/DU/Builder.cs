using Jint;
using System;
using System.Collections.Generic;
using System.Text;

namespace DUBuild.DU
{
    public class Builder
    {

        public Builder(System.IO.DirectoryInfo sourceDir, System.IO.DirectoryInfo outputDir, System.IO.FileInfo configFile, String outputFileName="out.json")
        {
            var Logger = NLog.LogManager.GetCurrentClassLogger();

            Logger.Info("Source path : {0}", sourceDir.FullName);
            Logger.Info("Destination path : {0}", outputDir.FullName);
            Logger.Info("Config path : {0}", configFile.FullName);
            Logger.Info("Output filename : {0}", outputFileName);

            if (!sourceDir.Exists)
            {
                throw new Exception("Source directory does not exist");
            }
            if (!outputDir.Exists)
            {
                outputDir.Create();
            }
            if (!configFile.Exists)
            {
                throw new Exception("Config file does not exist");
            }

            var configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<DU.Configuration>(configFile.OpenText().ReadToEnd());
            Logger.Info("Loaded build config successfully");

            var environmentVariables = Environment.GetEnvironmentVariables();

            var outputModule = new DU.OutputModule();

            var counter = 0;
            foreach (var slot in configuration.Slots)
            {
                var sb = new StringBuilder();
                if (slot.Code != null && slot.Code != String.Empty)
                {
                    var code = slot.Code;
                    foreach (var env in environmentVariables.Keys)
                    {
                        var value = environmentVariables[env];
                        code = code.Replace(env as string, value as string, StringComparison.InvariantCulture);
                    }

                    sb.Append(code);
                }
                else
                {
                    //Get source files
                    foreach (var sourceFile in slot.Files)
                    {
                        var sourcePath = System.IO.Path.Combine(sourceDir.FullName, sourceFile);
                        using (var sourceFileReader = new System.IO.StreamReader(sourcePath))
                        {
                            var sourceFileContents = sourceFileReader.ReadToEnd();

                            foreach (var env in environmentVariables.Keys)
                            {
                                var value = environmentVariables[env];
                                sourceFileContents = sourceFileContents.Replace(env as string, value as string, StringComparison.InvariantCulture);
                            }

                            if (configuration.Minify) sourceFileContents = Minify(sourceFileContents);
                            sb.Append(sourceFileContents);
                            sb.Append("\n");
                        }
                    }
                }

                outputModule.Handlers.Add(new OutputHandler()
                {
                    Code = sb.ToString(),
                    Filter = new OutputHandlerFilter()
                    {
                        Args = (slot.Args.Length > 0) ? new List<Dictionary<string, string>>() { new Dictionary<string, string>() { { "variable", slot.Args } } } : new List<Dictionary<string, string>>(),
                        Signature = slot.Signature,
                        SlotKey = slot.Slot
                    },
                    Key = counter.ToString()
                });

                counter++;
            }

            var outputFile = System.IO.Path.Combine(outputDir.FullName, outputFileName);
            var outputJsonData = Newtonsoft.Json.JsonConvert.SerializeObject(outputModule, Newtonsoft.Json.Formatting.Indented);
            Logger.Info("Writing output ({1} characters) to {0}", outputFile, outputJsonData.Length);
            System.IO.File.WriteAllText(outputFile, outputJsonData);
        }

        public string Minify(string source)
        {
            var jsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "js");

            var luaParse = System.IO.Path.Combine(jsDir, "luaparse.js");
            var luamin = System.IO.Path.Combine(jsDir, "luamin.js");

            var engine = new Engine().SetValue("source", source);
            engine.Execute(System.IO.File.ReadAllText(luaParse));
            engine.Execute(System.IO.File.ReadAllText(luamin));

            engine.Execute("minified = luamin.minify(source)");

            return engine.GetValue("minified").AsString();
        }
    }
}
