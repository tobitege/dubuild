using Jint;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DUBuild.DU
{
    public class Builder
    {
        private string ReplaceEnv(string code, System.Collections.IDictionary envKeys)
        {
            foreach (var env in envKeys.Keys)
            {
                var value = envKeys[env];
                code = code.Replace(env as string, value as string, StringComparison.InvariantCulture);
            }

            return code;
        }

        private NLog.ILogger Logger;

        public Utils.EnvContainer EnvironmentContainer { get; set; }
        public Utils.GitContainer GitContainer { get; set; }

        public System.IO.DirectoryInfo SourceDirectory { get; set; }
        public System.IO.DirectoryInfo OutputDir { get; set; }

        public Manifest Manifest { get; set; }

        public Builder()
        {
            Logger = NLog.LogManager.GetCurrentClassLogger();
        }
        public Builder(System.IO.DirectoryInfo sourceDir, System.IO.DirectoryInfo outputDir, Utils.EnvContainer environmentVariables)
            :this()
        {
            Logger.Info("Source path : {0}", sourceDir.FullName);
            Logger.Info("Destination path : {0}", outputDir.FullName);

            SourceDirectory = sourceDir;
            if (!SourceDirectory.Exists)
            {
                throw new Exception("Source directory does not exist");
            }
            OutputDir = outputDir;
            if (!outputDir.Exists)
            {
                outputDir.Create();
            }

            this.EnvironmentContainer = environmentVariables;

        }
        public Builder(System.IO.DirectoryInfo sourceDir, System.IO.DirectoryInfo outputDir, Utils.EnvContainer environmentVariables, Utils.GitContainer gitContainer)
            :this(sourceDir, outputDir, environmentVariables)
        {
            this.GitContainer = gitContainer;
        }
        public Builder(System.IO.DirectoryInfo sourceDir, System.IO.DirectoryInfo outputDir, System.IO.FileInfo manifestFile, Utils.EnvContainer environmentVariables, Utils.GitContainer gitContainer)
            :this(sourceDir, outputDir, environmentVariables, gitContainer)
        {
            Manifest = LoadManifest(manifestFile.FullName);
        }

        /// <summary>
        /// Constructs an output module and saves it
        /// Requires a manifest to be loaded, and the output path to be set
        /// </summary>
        /// <returns></returns>
        public bool ConstructAndSave()
        {
            if (Manifest == null) throw new Exception("Manifest has not been loaded");
            return ConstructAndSave(Manifest);
        }
        /// <summary>
        /// Constructs an output module and saves it
        /// </summary>
        /// <param name="manifest">A loaded manifest object, usually from json</param>
        /// <param name="outputFilename">The full path to the output file</param>
        /// <returns></returns>
        public bool ConstructAndSave(Manifest manifest)
        {
            var outputModule = ConstructOutputModule(manifest);
            return Save(outputModule, manifest.OutputFilename);
        }

        public OutputModule ConstructOutputModule(Manifest manifest)
        {
            var sourceRepository = ConstructSourceTree(this.SourceDirectory, this.GitContainer);

            var main = sourceRepository.GetByFilename(manifest.MainFile);
            if (main == null) return null;

            var dependencyTree = ConstructDependencyTree(main, sourceRepository);

            return Compile(dependencyTree);
        }
        /// <summary>
        /// Save the provided output module to the previously set output path
        /// </summary>
        /// <param name="outputModule"></param>
        /// <returns></returns>
        public bool Save(OutputModule outputModule)
        {
            return Save(outputModule, "out.json");
        }
        /// <summary>
        /// Save the provided output module to the designated output path
        /// </summary>
        /// <param name="outputModule"></param>
        /// <param name="outputFilename"></param>
        /// <returns></returns>
        public bool Save(OutputModule outputModule, string outputFilename)
        {
            var outputJsonData = Newtonsoft.Json.JsonConvert.SerializeObject(outputModule, Newtonsoft.Json.Formatting.Indented);
            var outputPath = System.IO.Path.Combine(OutputDir.FullName, outputFilename ?? "out.json");
            Logger.Info("Writing output ({1} characters) to {0}", outputPath, outputJsonData.Length);
            System.IO.File.WriteAllText(outputPath, outputJsonData);
            return true;
        }

        protected OutputModule Compile(DependencyTree dependencyTree)
        {
            var output = new OutputModule();

            //Add Dependencies + Main file
            foreach (var dependency in dependencyTree.GetDependencyOrder())
            { 
                var dependencySource = dependency.Key;
                var constructedHandler = ConstructOutputHandler(dependencySource, output.Handlers.Count, OutputModule.SlotKey.Unit);
                output.Handlers.Add(constructedHandler);
            }

            //Add proxies
            //Make sure to add after every construct, since construct uses the current size of the output handlers for the key
            Compile_AddProxies(output);

            return output;
        }
        protected OutputModule CompileMinified(DependencyTree dependencyTree)
        {
            var output = new OutputModule();
            var ob = new StringBuilder();

            //Add Dependencies + Main file
            foreach (var dependency in dependencyTree.GetDependencyOrder())
            {
                var dependencySource = dependency.Key;
                ob.Append(dependencySource.Contents);
                ob.Append(Environment.NewLine);
            }

            var constructedHandler = ConstructOutputHandler(Minify(ob.ToString()), output.Handlers.Count, OutputModule.SlotKey.Unit);
            output.Handlers.Add(constructedHandler);

            //Add proxies
            //Make sure to add after every construct, since construct uses the current size of the output handlers for the key
            Compile_AddProxies(output);

            return output;
        }

        protected OutputModule Compile_AddProxies(OutputModule output)
        {
            var unitStartHandler = ConstructOutputHandler("_G.BuildUnit.Start()", output.Handlers.Count, OutputModule.SlotKey.Unit, "start");
            output.Handlers.Add(unitStartHandler);
            var unitStopHandler = ConstructOutputHandler("_G.BuildUnit.Stop()", output.Handlers.Count, OutputModule.SlotKey.Unit, "stop");
            output.Handlers.Add(unitStopHandler);
            var systemActionStart = ConstructOutputHandler("_G.BuildSystem.ActionStart(action)", output.Handlers.Count, OutputModule.SlotKey.System, "actionStart(action)", "*");
            output.Handlers.Add(systemActionStart);
            var systemActionStop = ConstructOutputHandler("_G.BuildSystem.ActionStop(action)", output.Handlers.Count, OutputModule.SlotKey.System, "actionStop(action)", "*");
            output.Handlers.Add(systemActionStop);
            var systemUpdate = ConstructOutputHandler("_G.BuildSystem.Update()", output.Handlers.Count, OutputModule.SlotKey.System, "update");
            output.Handlers.Add(systemUpdate);
            var systemFlush = ConstructOutputHandler("_G.BuildSystem.Flush()", output.Handlers.Count, OutputModule.SlotKey.System, "flush");
            output.Handlers.Add(systemFlush);

            return output;
        }

        protected SourceRepository ConstructSourceTree(System.IO.DirectoryInfo sourceDirectory, Utils.GitContainer gitContainer)
        {
            var sourceRepository = new SourceRepository();
            foreach (var sourceFileRaw in sourceDirectory.EnumerateFiles("*.lua", System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    if (gitContainer != null)
                    {
                        var sourceFile = SourceFile.Parse(sourceFileRaw, gitContainer);
                        sourceRepository.Add(sourceFile);
                    }
                    else
                    {
                        var sourceFile = SourceFile.Parse(sourceFileRaw);
                        sourceRepository.Add(sourceFile);
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error processing {0}, {1}", sourceFileRaw.Name, e.Message);
                }
            }
            return sourceRepository;
        }
        protected DependencyTree ConstructDependencyTree(SourceFile main, SourceRepository sourceFiles)
        {
            var dependencyTree = new DependencyTree(sourceFiles);
            dependencyTree.Add(main);

            return dependencyTree;
        }
        protected OutputHandler ConstructOutputHandler(SourceFile source, int handlersCount, OutputModule.SlotKey slotKey, string method = "start", string argument = "")
        {
            //Semi dirty hack to add an environment variable
            EnvironmentContainer["CI_FILE_LAST_COMMIT"] = source.GitHash;

            return new OutputHandler()
            {
                Code = ReplaceEnv(source.Contents, EnvironmentContainer),
                Filter = new OutputHandlerFilter()
                {
                    Args = (argument.Length > 0) ? new List<Dictionary<string, string>>() { new Dictionary<string, string>() { { "variable", argument } } } : new List<Dictionary<string, string>>(),
                    Signature = method,
                    SlotKey = slotKey
                },
                Key = $"{handlersCount}"
            };
        }
        protected OutputHandler ConstructOutputHandler(string source, int handlersCount, OutputModule.SlotKey slotKey, string method = "start", string argument = "")
        {
            return new OutputHandler()
            {
                Code = ReplaceEnv(source, EnvironmentContainer),
                Filter = new OutputHandlerFilter()
                {
                    Args = (argument.Length > 0) ? new List<Dictionary<string, string>>() { new Dictionary<string, string>() { { "variable", argument } } } : new List<Dictionary<string, string>>(),
                    Signature = method,
                    SlotKey = slotKey
                },
                Key = $"{handlersCount}"
            };
        }

        protected string Minify(string source)
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
        public Manifest LoadManifest(string manifestFile)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(System.IO.File.ReadAllText(manifestFile));
        }
    }
}
