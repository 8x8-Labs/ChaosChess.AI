using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace ChaosChess.AI.Tests.Release;

public sealed class UnityPackageTests
{
    [Fact]
    public void PackageUnityScript_CreatesManifestAndChecksumsForBuiltArtifact()
    {
        string repositoryRoot = FindRepositoryRoot();
        string outputRoot = Path.Combine(
            Path.GetTempPath(),
            "ChaosChess.AI.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            RunPackageScript(repositoryRoot, outputRoot);

            string packageDirectory = Path.Combine(
                outputRoot,
                "ChaosChess.AI-v0.1.0-unity");
            string packageZip = packageDirectory + ".zip";
            string dllPath = Path.Combine(packageDirectory, "ChaosChess.AI.dll");
            string pdbPath = Path.Combine(packageDirectory, "ChaosChess.AI.pdb");
            string xmlPath = Path.Combine(packageDirectory, "ChaosChess.AI.xml");
            string manifestPath = Path.Combine(packageDirectory, "manifest.json");
            string sumsPath = Path.Combine(packageDirectory, "SHA256SUMS.txt");

            Assert.True(File.Exists(packageZip), "Package zip was not created.");
            Assert.True(File.Exists(dllPath), "DLL was not packaged.");
            Assert.True(File.Exists(pdbPath), "PDB was not packaged.");
            Assert.True(File.Exists(xmlPath), "XML documentation was not packaged.");
            Assert.True(File.Exists(manifestPath), "Manifest was not packaged.");
            Assert.True(File.Exists(sumsPath), "SHA256SUMS was not packaged.");

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            JsonElement root = manifest.RootElement;
            Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("ChaosChess.AI", root.GetProperty("name").GetString());
            Assert.Equal("0.1.0", root.GetProperty("version").GetString());
            Assert.Equal("v0.1.0", root.GetProperty("tag").GetString());
            Assert.Equal("netstandard2.1", root.GetProperty("targetFramework").GetString());
            Assert.Equal("ChaosChess.AI.dll", root.GetProperty("assembly").GetString());
            Assert.Matches("^[0-9a-f]{40}$", root.GetProperty("commitSha").GetString());

            IReadOnlyDictionary<string, FileEntry> files = root.GetProperty("files")
                .EnumerateArray()
                .Select(entry => new FileEntry(
                    entry.GetProperty("path").GetString()!,
                    entry.GetProperty("sha256").GetString()!,
                    entry.GetProperty("size").GetInt64()))
                .ToDictionary(entry => entry.Path, StringComparer.Ordinal);

            Assert.Equal(GetSha256(dllPath), files["ChaosChess.AI.dll"].Sha256);
            Assert.Equal(GetSha256(pdbPath), files["ChaosChess.AI.pdb"].Sha256);
            Assert.Equal(GetSha256(xmlPath), files["ChaosChess.AI.xml"].Sha256);
            Assert.Equal(new FileInfo(dllPath).Length, files["ChaosChess.AI.dll"].Size);

            string sums = File.ReadAllText(sumsPath);
            Assert.Contains(GetSha256(dllPath) + "  ChaosChess.AI.dll", sums);
            Assert.Contains(GetSha256(manifestPath) + "  manifest.json", sums);

            AssemblyName assemblyName = AssemblyName.GetAssemblyName(dllPath);
            Assert.Equal("ChaosChess.AI", assemblyName.Name);
            Assert.Equal(new Version(0, 1, 0, 0), assemblyName.Version);

            VerifyAssemblyMetadata(dllPath);
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void PackageUnityScript_RejectsInvalidVersion()
    {
        string repositoryRoot = FindRepositoryRoot();
        string outputRoot = Path.Combine(
            Path.GetTempPath(),
            "ChaosChess.AI.Tests",
            Guid.NewGuid().ToString("N"));
        string shell = GetPowerShellExecutable();
        string script = Path.Combine(repositoryRoot, "scripts", "package-unity.ps1");

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = shell,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot
        }.WithArguments(
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            script,
            "-Version",
            "v0.1.0",
            "-OutputRoot",
            outputRoot,
            "-NoBuild"));

        Assert.NotNull(process);
        process!.WaitForExit(30000);
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        Assert.NotEqual(0, process.ExitCode);
        Assert.Contains("Version must be SemVer without the leading v", output);
    }

    private static void RunPackageScript(string repositoryRoot, string outputRoot)
    {
        string shell = GetPowerShellExecutable();
        string script = Path.Combine(repositoryRoot, "scripts", "package-unity.ps1");
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = shell,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot
        }.WithArguments(
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            script,
            "-Version",
            "0.1.0",
            "-OutputRoot",
            outputRoot,
            "-NoBuild"));

        Assert.NotNull(process);
        process!.WaitForExit(60000);
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        Assert.True(process.ExitCode == 0, output);
    }

    private static void VerifyAssemblyMetadata(string dllPath)
    {
        var context = new AssemblyLoadContext("ChaosChess.AI package test", isCollectible: true);
        try
        {
            using FileStream stream = File.OpenRead(dllPath);
            Assembly assembly = context.LoadFromStream(stream);
            string frameworkName = assembly
                .GetCustomAttribute<TargetFrameworkAttribute>()!
                .FrameworkName;
            Assert.Equal(".NETStandard,Version=v2.1", frameworkName);

            string[] forbiddenReferences =
            {
                "Unity" + "Engine",
                "DO" + "Tween",
                "Fairy" + "StockfishBridge",
                "ChaosChess.AI.Stockfish"
            };

            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
            {
                Assert.DoesNotContain(
                    forbiddenReferences,
                    forbidden => string.Equals(reference.Name, forbidden, StringComparison.Ordinal));
            }
        }
        finally
        {
            context.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    private static string GetSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        byte[] hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }

    private static string GetPowerShellExecutable()
    {
        return OperatingSystem.IsWindows()
            ? "powershell.exe"
            : "pwsh";
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ChaosChess.AI.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate ChaosChess.AI.sln.");
    }

    private sealed record FileEntry(string Path, string Sha256, long Size);
}

internal static class ProcessStartInfoExtensions
{
    public static ProcessStartInfo WithArguments(
        this ProcessStartInfo startInfo,
        params string[] arguments)
    {
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
