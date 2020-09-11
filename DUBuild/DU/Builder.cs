using Jint;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DUBuild.DU
{
    public class Builder
    {

        private OutputHandler GenerateSlot(string code, string args, string signature, string slot, int counter)
        {
            return new OutputHandler()
            {
                Code = code,
                Filter = new OutputHandlerFilter()
                {
                    Args = (args.Length > 0) ? new List<Dictionary<string, string>>() { new Dictionary<string, string>() { { "variable", args } } } : new List<Dictionary<string, string>>(),
                    Signature = signature,
                    SlotKey = slot
                },
                Key = counter.ToString()
            };
        }
        private string ReplaceEnv(string code, System.Collections.IDictionary envKeys)
        {
            foreach (var env in envKeys.Keys)
            {
                var value = envKeys[env];
                code = code.Replace(env as string, value as string, StringComparison.InvariantCulture);
            }

            return code;
        }

        public Builder(System.IO.DirectoryInfo sourceDir, System.IO.DirectoryInfo outputDir, System.IO.FileInfo configFile, Utils.EnvContainer environmentVariables, Utils.GitContainer gitContainer, String outputFileName = "out.json")
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

            var outputModule = new DU.OutputModule();

            var counter = 0;
            foreach (var slot in configuration.Slots)
            {
                var sb = new StringBuilder();
                if (slot.Code != null && slot.Code != String.Empty)
                {
                    var code = slot.Code;
                    foreach (var env in environmentVariables)
                    {
                        code = code.Replace(env.Key, env.Value, StringComparison.InvariantCulture);
                    }

                    sb.Append(code);
                    outputModule.Handlers.Add(GenerateSlot(sb.ToString(), slot.Args, slot.Signature, slot.Slot, counter));
                }
                else if (slot.Directory != null & slot.Directory != String.Empty)
                {
                    var fileInfo = new System.IO.FileInfo(slot.Directory);
                    var filePattern = fileInfo.Name;
                    var directoryInfo = fileInfo.Directory;
                    foreach (var file in directoryInfo.EnumerateFiles(filePattern))
                    {
                        var relativePath = gitContainer.ConvertToRelative(sourceDir.FullName, file.FullName);
                        environmentVariables.Remove("CI_FILE_LAST_COMMIT");
                        var lastModifiyingHash = gitContainer.GetFileLastModifiedCommit(relativePath);
                        environmentVariables["CI_FILE_LAST_COMMIT"] = lastModifiyingHash;

                        sb.Clear();
                        using (var sourceFileReader = new System.IO.StreamReader(file.FullName))
                        {
                            var sourceFileContents = sourceFileReader.ReadToEnd();

                            sourceFileContents = ReplaceEnv(sourceFileContents, environmentVariables);

                            if (configuration.Minify) sourceFileContents = Minify(sourceFileContents);
                            sb.Append(sourceFileContents);

                            outputModule.Handlers.Add(GenerateSlot(sb.ToString(), slot.Args, slot.Signature, slot.Slot, counter));
                            counter++;
                        }
                    }
                }
                else
                {
                    //Get source files
                    foreach (var sourceFile in slot.Files)
                    {
                        var sourcePath = System.IO.Path.Combine(sourceDir.FullName, sourceFile);
                        environmentVariables.Remove("CI_FILE_LAST_COMMIT");
                        var lastModifiyingHash = gitContainer.GetFileLastModifiedCommit(sourceFile);
                        environmentVariables["CI_FILE_LAST_COMMIT"] = lastModifiyingHash;

                        using (var sourceFileReader = new System.IO.StreamReader(sourcePath))
                        {
                            var sourceFileContents = sourceFileReader.ReadToEnd();

                            sourceFileContents = ReplaceEnv(sourceFileContents, environmentVariables);

                            if (configuration.Minify) sourceFileContents = Minify(sourceFileContents);
                            sb.Append(sourceFileContents);
                            sb.Append("\n");
                        }
                    }
                    outputModule.Handlers.Add(GenerateSlot(sb.ToString(), slot.Args, slot.Signature, slot.Slot, counter));
                }
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
