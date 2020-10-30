using Microsoft.Extensions.CommandLineUtils;
using System;

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

            app.Command("build", (command) =>{
                command.Description = "Build a DU script";

                var sourceDir = command.Option("-s|--source", "Source files location", CommandOptionType.SingleValue);
                var outputDir = command.Option("-o|--output", "Output location", CommandOptionType.SingleValue);
                var mainFile = command.Option("-m|--main", "Main file location", CommandOptionType.SingleValue);
                var warningAsErrors = command.Option("-w|--warningsaserrors", "Treat warnings as errors", CommandOptionType.SingleValue);

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
                    } catch (Exception e) { logger.Warn(e, "Error loading git container, ignoring - Git hashes will not be available"); }

                    var manifestFileInfo = new System.IO.FileInfo(mainFile.Value());
                    foreach (var matchingFile in manifestFileInfo.Directory.EnumerateFiles(manifestFileInfo.Name))
                    {
                        var builder = new DU.Builder(
                        new System.IO.DirectoryInfo(sourceDir.Value()),
                        new System.IO.DirectoryInfo(outputDir.Value()),
                        matchingFile,
                        envContainer,
                        gitContainer
                        );

                        if (warningAsErrors.HasValue()) builder.TreatWarningsAsErrors = bool.Parse(warningAsErrors.Value());

                        builder.ConstructAndSave();
                    }

                    return 0;
                });

            });
            app.Execute(args);

        }
    }
}
