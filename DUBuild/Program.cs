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
                var configFile = command.Option("-c|--config", "Output location", CommandOptionType.SingleValue);
                var outFileName = command.Option("-f|--filename", "Output file name", CommandOptionType.SingleValue);

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
                    if (!configFile.HasValue())
                    {
                        logger.Error("Missing config file");
                        app.ShowHint();
                        Environment.Exit(2);
                    }

                    var envContainer = new Utils.EnvContainer();
                    var gitContainer = new Utils.GitContainer(sourceDir.Value());

                    var builder = new DU.Builder(
                        new System.IO.DirectoryInfo(sourceDir.Value()),
                        new System.IO.DirectoryInfo(outputDir.Value()),
                        new System.IO.FileInfo(configFile.Value()),
                        envContainer,
                        gitContainer,
                        outFileName.HasValue() ? outFileName.Value() : "out.json"
                        );

                    return 0;
                });

            });
            app.Execute(args);

        }
    }
}
