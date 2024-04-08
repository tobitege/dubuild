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
        FileInfo BadConfigFile;
        DUBuild.Utils.EnvContainer EnvironmentContainer;

        FileInfo OutFile;
        FileInfo OutFileMinified;


        [OneTimeSetUp]
        public void Setup()
        {
            RootDir = AppDomain.CurrentDomain.BaseDirectory;
            SourceDir = new System.IO.DirectoryInfo(System.IO.Path.Combine(RootDir, "source"));
            OutDir = new System.IO.DirectoryInfo(System.IO.Path.Combine(RootDir, "dest"));
            ConfigFile = new System.IO.FileInfo(System.IO.Path.Combine(SourceDir.FullName, "main.lua"));
            BadConfigFile = new System.IO.FileInfo(System.IO.Path.Combine(SourceDir.FullName, "main_fails.lua"));
            OutFile = new FileInfo(Path.Combine(OutDir.FullName, "out.json"));
            OutFileMinified = new FileInfo(Path.Combine(OutDir.FullName, "out.min.json"));
            EnvironmentContainer = new DUBuild.Utils.EnvContainer();
        }

        [Test]
        public void CompilesCorrectly()
        {
            var b1 = new DUBuild.DU.Builder(SourceDir, new System.Collections.Generic.List<DirectoryInfo>(), OutDir, ConfigFile, EnvironmentContainer, null);
            Assert.DoesNotThrow(() => b1.ConstructAndSave(false));
            Assert.DoesNotThrow(() => b1.ConstructAndSave(true));
            Assert.That(OutFile.Exists);
            Assert.That(OutFileMinified.Exists);

        }

        [Test]
        public void OutputContainsDUElements()
        {
            var source = new StreamReader(OutFile.OpenRead()).ReadToEnd();
            var sourceMinified = new StreamReader(OutFileMinified.OpenRead()).ReadToEnd();

            Assert.That(source.Contains("\"slots\": {"));
            Assert.That(source.Contains("\"2\": {"));
            Assert.That(source.Contains("\"name\": \"library\","));
            Assert.That(source.Contains("\"handlers\": ["));
            Assert.That(sourceMinified.Contains("\"slots\": {"));
            Assert.That(sourceMinified.Contains("\"2\": {"));
            Assert.That(sourceMinified.Contains("\"name\": \"library\","));
            Assert.That(sourceMinified.Contains("\"handlers\": ["));
        }

        [Test]
        public void MinifiedCorrectly()
        {
            var sourceMinified = new StreamReader(OutFileMinified.OpenRead()).ReadToEnd();
            Assert.That(!sourceMinified.Contains("--This is a test"));
        }

        [Test]
        public void ContainsCompiledCode()
        {
            var source = new StreamReader(OutFile.OpenRead()).ReadToEnd();
            var sourceMinified = new StreamReader(OutFileMinified.OpenRead()).ReadToEnd();
            Assert.That(source.Contains("_G.BuildUnit = {}"));
            Assert.That(sourceMinified.Contains("_G.BuildUnit={}"));
        }

        [Test]
        public void MissingDependencyFails()
        {
            var b1 = new DUBuild.DU.Builder(SourceDir, new System.Collections.Generic.List<DirectoryInfo>(), OutDir, BadConfigFile, EnvironmentContainer, null);
            Assert.Throws(typeof(DUBuild.DU.DependencyTree.MissingDependencyException), () => b1.ConstructAndSave(false));
            Assert.Throws(typeof(DUBuild.DU.DependencyTree.MissingDependencyException), () => b1.ConstructAndSave(true));
        }
    }
}