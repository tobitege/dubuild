using Jint;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using static DUBuild.DU.OutputModule;
using System.IO;

namespace DUBuild.DU
{
	public class Builder
	{
		private string ReplaceEnv(string code, System.Collections.IDictionary envKeys)
		{
			foreach (var env in envKeys.Keys)
			{
				var value = envKeys[env];
				code = code.Replace($"%{env}%" as string, value as string, StringComparison.InvariantCulture);
			}

			return code;
		}

		private NLog.ILogger Logger;

		public bool TreatWarningsAsErrors { get; set; }

		public Utils.EnvContainer EnvironmentContainer { get; set; }
		public Utils.GitContainer GitContainer { get; set; }

		public System.IO.DirectoryInfo SourceDirectory { get; set; }
		public List<System.IO.DirectoryInfo> ExcludeDirectories { get; set; }
		public System.IO.DirectoryInfo OutputDir { get; set; }

		public System.IO.FileInfo MainFile { get; set; }

		public string OutputName { get; private set; }
		public string OutputNameMinified { get; private set; }

		public Builder()
		{
			Logger = NLog.LogManager.GetCurrentClassLogger();
			TreatWarningsAsErrors = true;
			OutputName = "";
			OutputNameMinified = "";
		}
		private Builder(System.IO.DirectoryInfo sourceDir, List<System.IO.DirectoryInfo> excludeDirs, System.IO.DirectoryInfo outputDir, Utils.EnvContainer environmentVariables)
			:this()
		{
			Logger.Info("Source path : {0}", sourceDir.FullName);
			Logger.Info("Destination path : {0}", outputDir.FullName);
			Logger.Info("Exclude paths : {0}", string.Join(", ", excludeDirs.Select(x=>x.FullName)));

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

			this.ExcludeDirectories = excludeDirs;

			this.EnvironmentContainer = environmentVariables;

		}
		private Builder(System.IO.DirectoryInfo sourceDir, List<System.IO.DirectoryInfo> excludeDirs, System.IO.DirectoryInfo outputDir, Utils.EnvContainer environmentVariables, Utils.GitContainer gitContainer)
			:this(sourceDir, excludeDirs, outputDir, environmentVariables)
		{
			this.GitContainer = gitContainer;
		}
		public Builder(System.IO.DirectoryInfo sourceDir, List<System.IO.DirectoryInfo> excludeDirs, System.IO.DirectoryInfo outputDir, System.IO.FileInfo mainFile, Utils.EnvContainer environmentVariables, Utils.GitContainer gitContainer)
			:this(sourceDir, excludeDirs, outputDir, environmentVariables, gitContainer)
		{
			MainFile = mainFile;
			Logger.Info("Main file : {0}", mainFile.FullName);
		}

		private string GetFilename(SourceFile main, bool minified)
		{
			var filename = main.OutFilename ?? "out.json";
			if (minified)
			{
				var extension = new System.IO.FileInfo(filename).Extension;
				filename = filename.Replace(extension, $".min{extension}");
			}
			return filename;
		}

		/// <summary>
		/// Constructs an output module and saves it
		/// Requires a manifest to be loaded, and the output path to be set
		/// </summary>
		/// <returns></returns>
		public bool ConstructAndSave(bool minified)
		{
			var sourceRepository = ConstructSourceTree(this.SourceDirectory, this.GitContainer);

			var main = sourceRepository.GetByFilename(MainFile.Name);
			if (main == null) return false;

			var dependencyTree = ConstructDependencyTree(main, sourceRepository);

			var om = minified ? CompileMinified(dependencyTree, main) : Compile(dependencyTree, main);

			var outputFilename = System.IO.Path.Combine(OutputDir.FullName, GetFilename(main, minified));
			if (minified)
			{
				OutputNameMinified = outputFilename;
			}
			else
			{
				OutputName = outputFilename;
			}
			return Save(om, outputFilename);
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
			Logger.Info("Writing ({1} characters) to {0}", outputFilename, outputJsonData.Length);
			try
			{
				System.IO.File.WriteAllText(outputFilename, outputJsonData);
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			return false;
		}

		protected OutputModule Compile(DependencyTree dependencyTree, SourceFile mainFile)
		{
			var output = new OutputModule();
			string moduleIndex = "_G._ModuleIndex={}\r\n";

			//Add Dependencies + Main file
			foreach (var dependency in dependencyTree.GetDependencyOrder())
			{
				var dependencySource = dependency.Key;
				var constructedHandler = ConstructOutputHandler(dependencySource, output.Handlers.Count, SlotKey.Unit);
				moduleIndex += $"_G._ModuleIndex[{output.Handlers.Count}]='{Path.GetFileName(dependencySource.File.ToString())}';";
				output.Handlers.Add(constructedHandler);
			}

			//Add proxies
			//Make sure to add after every construct, since construct uses the current size of the output handlers for the key
			Compile_AddProxies(output, mainFile);

			output.Handlers.Add(new OutputHandler() {
				Key = "1",
				Filter = new OutputHandlerFilter()
				{
					Signature = "onStart",
					SlotKey = SlotKey.Library,
					Args = new List<Dictionary<string, string>>()
				},
				Code = moduleIndex
			});

			return output;
		}
		protected OutputModule CompileMinified(DependencyTree dependencyTree, SourceFile mainFile)
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
			Compile_AddProxies(output, mainFile);

			return output;
		}

		protected OutputModule Compile_AddProxies(OutputModule output, SourceFile mainFile)
		{
			var unitStartHandler = ConstructOutputHandler("_G.BuildUnit.onStart()", output.Handlers.Count, OutputModule.SlotKey.Unit, "onStart");
			output.Handlers.Add(unitStartHandler);
			var unitStopHandler = ConstructOutputHandler("_G.BuildUnit.onStop()", output.Handlers.Count, OutputModule.SlotKey.Unit, "onStop");
			output.Handlers.Add(unitStopHandler);
			var systemActionStart = ConstructOutputHandler("_G.BuildSystem.onActionStart(action)", output.Handlers.Count, OutputModule.SlotKey.System, "onActionStart(action)", new string[]{ "*" });
			output.Handlers.Add(systemActionStart);
			var systemActionStop = ConstructOutputHandler("_G.BuildSystem.onActionStop(action)", output.Handlers.Count, OutputModule.SlotKey.System, "onActionStop(action)", new string[] { "*" });
			output.Handlers.Add(systemActionStop);
			var inputText = ConstructOutputHandler("_G.BuildSystem.onInputText(action)", output.Handlers.Count, OutputModule.SlotKey.System, "onInputText(action)", new string[] { "*" });
			output.Handlers.Add(inputText);
			var systemUpdate = ConstructOutputHandler("_G.BuildSystem.onUpdate()", output.Handlers.Count, OutputModule.SlotKey.System, "onUpdate");
			output.Handlers.Add(systemUpdate);
			var systemFlush = ConstructOutputHandler("_G.BuildSystem.onFlush()", output.Handlers.Count, OutputModule.SlotKey.System, "onFlush");
			output.Handlers.Add(systemFlush);

			foreach (var timer in mainFile.Timers ?? new List<string>())
			{
				var timerTick = ConstructOutputHandler($"_G.BuildUnit.onTimer(\"{timer}\")", output.Handlers.Count, OutputModule.SlotKey.Unit, "onTimer(timerId)", new string[] { timer });
				output.Handlers.Add(timerTick);
			}

			//Commence jank
			for(int i = 0; i < 10; i++)
			{
				var slot_Emitter = ConstructOutputHandler($"_G.BuildEmitter.onSent(channel, message, slot{i+1})", output.Handlers.Count, new OutputModule.SlotKey(i), "onSent(channel,message)", new string[] { "*", "*" });
				output.Handlers.Add(slot_Emitter);
				var slot_Receiver = ConstructOutputHandler($"_G.BuildReceiver.onReceived(channel, message, slot{i+1})", output.Handlers.Count, new OutputModule.SlotKey(i), "onReceived(channel,message)", new string[] { "*", "*" });
				output.Handlers.Add(slot_Receiver);
				var slot_ScreenDown = ConstructOutputHandler($"_G.BuildScreen.onMouseDown(x, y, slot{i+1})", output.Handlers.Count, new OutputModule.SlotKey(i), "onMouseDown(x,y)", new string[] { "*", "*" });
				output.Handlers.Add(slot_ScreenDown);
				var slot_ScreenUp = ConstructOutputHandler($"_G.BuildScreen.onMouseUp(x, y, slot{i+1})", output.Handlers.Count, new OutputModule.SlotKey(i), "onMouseUp(x,y)", new string[] { "*", "*" });
				output.Handlers.Add(slot_ScreenUp);
			}

			return output;
		}

		protected SourceRepository ConstructSourceTree(System.IO.DirectoryInfo sourceDirectory, Utils.GitContainer gitContainer)
		{
			var sourceRepository = new SourceRepository();
			var errors = new List<Exception>();

			foreach (var sourceFileRaw in sourceDirectory.EnumerateFiles("*.lua", System.IO.SearchOption.AllDirectories))
			{
				try
				{
					if (ExcludeDirectories.Any(x => sourceFileRaw.Directory.FullName.Contains(x.FullName))) continue;

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
					if (TreatWarningsAsErrors)
					{
						errors.Add(e);
					}
					Logger.Error("Error processing {0}, {1}", sourceFileRaw.Name, e.Message);
				}
			}

			if (errors.Count > 0)
			{
				throw new Exception("There were errors processing the source files");
			}

			return sourceRepository;
		}
		protected DependencyTree ConstructDependencyTree(SourceFile main, SourceRepository sourceFiles)
		{
			var dependencyTree = new DependencyTree(sourceFiles);
			dependencyTree.Add(main);

			return dependencyTree;
		}

		protected OutputHandler ConstructOutputHandler(SourceFile source, int handlersCount, OutputModule.SlotKey slotKey, string method = "onStart", string[] arguments = null)
		{
			//Semi dirty hack to add an environment variable
			EnvironmentContainer["GIT_FILE_LAST_COMMIT"] = source.GitHash;

			var args = new List<Dictionary<string, string>>();
			foreach (var str in arguments ?? new string[] { }) args.Add(new Dictionary<string, string>() { { "variable", str } });

			return new OutputHandler()
			{
				Code = ReplaceEnv(source.Contents, EnvironmentContainer),
				Filter = new OutputHandlerFilter()
				{
					Args = args,
					Signature = method,
					SlotKey = slotKey
				},
				Key = $"{handlersCount}"
			};
		}
		protected OutputHandler ConstructOutputHandler(string source, int handlersCount, OutputModule.SlotKey slotKey, string method = "onStart", string[] arguments = null)
		{
			var args = new List<Dictionary<string, string>>();
			foreach (var str in arguments?? new string[] { }) args.Add(new Dictionary<string, string>() { { "variable", str } });

			return new OutputHandler()
			{
				Code = ReplaceEnv(source, EnvironmentContainer),
				Filter = new OutputHandlerFilter()
				{
					Args = args,
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
	}
}
