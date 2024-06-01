using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using TextCopy;

namespace DUBuild
{
	class Program
	{
		static void Main(string[] args)
		{
			var logger = NLog.LogManager.GetCurrentClassLogger();
			logger.Info("Starting DUBuild");

			var rootDir = AppDomain.CurrentDomain.BaseDirectory;
			var versionHash = System.IO.File.ReadAllText(System.IO.Path.Combine(rootDir, "versionhash"));

			logger.Info($"Version {versionHash}");

			var app = new CommandLineApplication();
			app.Name = "DUBuild";
			app.Description = "Dual Universe script compiler";
			app.HelpOption("-?|-h|--help");

			app.Command("build", (command) =>
			{
				command.Description = "Build a DU script";

				var sourceDir = command.Option("-s|--source", "Source files location", CommandOptionType.SingleValue);
				var outputDir = command.Option("-o|--output", "Output location", CommandOptionType.SingleValue);
				var mainFile = command.Option("-m|--main", "Main file location", CommandOptionType.SingleValue);
				var excludedDirectories = command.Option("-e|--exclude", "Comma separated list of directories to exclude from the source tree", CommandOptionType.SingleValue);
				var warningAsErrors = command.Option("-w|--warningsaserrors", "Treat warnings as errors", CommandOptionType.SingleValue);
				var skipMinify = command.Option("-nm|--no-minify", "Skip minification", CommandOptionType.NoValue);
				var copyToClipboardFull = command.Option("-cF|--copyFull", "Copy normal output file to clipboard", CommandOptionType.NoValue);
				var copyToClipboardMin = command.Option("-cM|--copyMin", "Copy minified output file to clipboard", CommandOptionType.NoValue);

				command.OnExecute(() =>
				{
					if (!sourceDir.HasValue())
					{
						logger.Error("Missing source directory");
						app.ShowHint();
						Environment.Exit(2);
					}
					if (!outputDir.HasValue())
					{
						logger.Error("Missing output directory");
						app.ShowHint();
						Environment.Exit(2);
					}
					if (!mainFile.HasValue())
					{
						logger.Error("Missing main file");
						app.ShowHint();
						Environment.Exit(2);
					}

					var envContainer = new Utils.EnvContainer();
					Utils.GitContainer gitContainer = null;
					try
					{
						gitContainer = new Utils.GitContainer(sourceDir.Value());
					}
					catch (Exception e) { logger.Warn(e, "Error loading git container, ignoring - Git hashes will not be available"); }

					var excludeDirectories = new System.Collections.Generic.List<System.IO.DirectoryInfo>();
					if (excludedDirectories.HasValue())
					{
						var ex = excludedDirectories.Value();
						if (ex.Contains(","))
						{
							var exSplit = ex.Split(",");
							foreach (var exSplitPath in exSplit) excludeDirectories.Add(new System.IO.DirectoryInfo(exSplitPath));
						}
						else
						{
							excludeDirectories.Add(new System.IO.DirectoryInfo(ex));
						}
					}

					var manifestFileInfo = new System.IO.FileInfo(mainFile.Value());
					foreach (var matchingFile in manifestFileInfo.Directory.EnumerateFiles(manifestFileInfo.Name))
					{

						var builder = new DU.Builder(
							new System.IO.DirectoryInfo(sourceDir.Value()),
							excludeDirectories,
							new System.IO.DirectoryInfo(outputDir.Value()),
							matchingFile,
							envContainer,
							gitContainer
						);

						if (warningAsErrors.HasValue()) builder.TreatWarningsAsErrors = bool.Parse(warningAsErrors.Value());
						logger.Info("Treating warnings as errors? {0}", builder.TreatWarningsAsErrors);

						builder.ConstructAndSave(false);
						if (!skipMinify.HasValue())
						{
							builder.ConstructAndSave(true);
						}

						if (copyToClipboardFull.HasValue() || copyToClipboardMin.HasValue())
						{
							var filename = copyToClipboardFull.HasValue() ? builder.OutputName : builder.OutputNameMinified;
							if (!string.IsNullOrEmpty(filename) && File.Exists(filename))
							{
								var outputFileContent = System.IO.File.ReadAllText(filename);
								ClipboardService.SetText(outputFileContent);
								logger.Info("Output file copied to clipboard.");
							}
						}
					}
					return 0;
				});

			});
			app.Execute(args);
		}
	}
}
