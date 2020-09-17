using NUnit.Framework;
using System;
using System.IO;

namespace DUBuild_Tests
{
    public class Tests
    {
        String RootDir;
        DirectoryInfo SourceDir;
        DirectoryInfo OutDir;
        FileInfo ConfigFile;
        FileInfo ConfigFileMinified;
        String OutFile;
        String OutFileMinified;

        String Source { get; set; }
        String SourceMinified { get; set; }


        [OneTimeSetUp]
        public void Setup()
        {
            RootDir = AppDomain.CurrentDomain.BaseDirectory;
            SourceDir = new System.IO.DirectoryInfo(System.IO.Path.Combine(RootDir, "source"));
            OutDir = new System.IO.DirectoryInfo(System.IO.Path.Combine(RootDir, "dest"));
            ConfigFile = new System.IO.FileInfo(System.IO.Path.Combine(RootDir, "config", "buildConfig.json"));
            ConfigFileMinified = new System.IO.FileInfo(System.IO.Path.Combine(RootDir, "config", "buildConfigMinified.json"));
            OutFile = System.IO.Path.Combine(OutDir.FullName, "out.json");
            OutFileMinified = System.IO.Path.Combine(OutDir.FullName, "outMinified.json");
            var envContainer = new DUBuild.Utils.EnvContainer();
            new DUBuild.DU.Builder(SourceDir, OutDir, ConfigFile, envContainer, null, "out.json");
            new DUBuild.DU.Builder(SourceDir, OutDir, ConfigFileMinified, envContainer, null, "outMinified.json");


            if (System.IO.File.Exists(OutFile))
                using (var reader = new System.IO.StreamReader(OutFile))
                {
                    Source = reader.ReadToEnd();
                }
            if (System.IO.File.Exists(OutFileMinified))
                using (var reader = new System.IO.StreamReader(OutFileMinified))
                {
                    SourceMinified = reader.ReadToEnd();
                }
        }

        [Test, Order(1)]
        public void OutputExists()
        {
            Assert.IsTrue(System.IO.File.Exists(OutFile));
            Assert.IsTrue(System.IO.File.Exists(OutFileMinified));
        }

        [Test]
        public void OutputCorrect()
        {
            Assert.IsTrue(Source.Contains("\"slots\": {"));
            Assert.IsTrue(Source.Contains("\"2\": {"));
            Assert.IsTrue(Source.Contains("\"name\": \"library\","));
            Assert.IsTrue(Source.Contains("\"handlers\": ["));
        }

        [Test]
        public void CommentsRemoved()
        {
            Assert.IsFalse(SourceMinified.Contains("--This is a test"));
        }
    }
}