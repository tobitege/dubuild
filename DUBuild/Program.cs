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

            var sourceDir = app.Option("-s|--source", "Source files location", CommandOptionType.SingleValue);
            var outputDir = app.Option("-o|--output", "Output location", CommandOptionType.SingleValue);
            var configFile = app.Option("-c|--config", "Output location", CommandOptionType.SingleValue);
            var shipID = app.Option("-i|--id", "Ship ID to encrypt for", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (!sourceDir.HasValue()) {
                    logger.Error("Missing source directory");
                    app.ShowHint();
                }
                if (!outputDir.HasValue())
                {
                    logger.Error("Missing output directory");
                    app.ShowHint();
                }
                if (!configFile.HasValue())
                {
                    logger.Error("Missing config file");
                    app.ShowHint();
                }

                var builder = new DU.Builder(
                    new System.IO.DirectoryInfo(sourceDir.Value()),
                    new System.IO.DirectoryInfo(outputDir.Value()),
                    new System.IO.FileInfo(configFile.Value())
                    );

                return 0;
            });

        }
    }
}
