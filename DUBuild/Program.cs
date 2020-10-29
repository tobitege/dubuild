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
                var manifestFile = command.Option("-m|--manifest", "Manifest file location", CommandOptionType.SingleValue);

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
                    if (!manifestFile.HasValue())
                    {
                        logger.Error("Missing manifest file");
                        app.ShowHint();
                        Environment.Exit(2);
                    }

                    var envContainer = new Utils.EnvContainer();
                    Utils.GitContainer gitContainer = null;
                    try
                    {
                        gitContainer = new Utils.GitContainer(sourceDir.Value());
                    } catch (Exception e) { logger.Warn(e, "Error loading git container, ignoring - Git hashes will not be available"); }

                    var builder = new DU.Builder(
                        new System.IO.DirectoryInfo(sourceDir.Value()),
                        new System.IO.DirectoryInfo(outputDir.Value()),
                        new System.IO.FileInfo(manifestFile.Value()),
                        envContainer,
                        gitContainer
                        );
                    builder.ConstructAndSave();

                    return 0;
                });

            });
            app.Execute(args);

        }
    }
}
