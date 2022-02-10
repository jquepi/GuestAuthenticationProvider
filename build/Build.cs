using System;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Tools.OctoVersion;

[CheckBuildProjectConfigurations]
class Build : NukeBuild
{
    [Parameter("Configuration to build - 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release;

    [Solution] readonly Solution Solution;
    [OctoVersion] readonly OctoVersionInfo OctoVersionInfo;

    AbsolutePath LocalPackagesDirectory => RootDirectory / ".." / "LocalPackages";
    AbsolutePath SourceDirectory => RootDirectory / "source";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PublishDirectory => RootDirectory / "publish";

    Target Clean =>
        _ => _
            .Executes(() =>
            {
                EnsureCleanDirectory(ArtifactsDirectory);
                EnsureCleanDirectory(PublishDirectory);
                SourceDirectory
                    .GlobDirectories("**/bin", "**/obj")
                    .ForEach(EnsureCleanDirectory);
            });

    Target Restore =>
        _ => _
            .DependsOn(Clean)
            .Executes(() =>
            {
                DotNetRestore(_ => _
                    .SetProjectFile(Solution));
            });

    Target Compile =>
        _ => _
            .DependsOn(Restore)
            .Executes(() =>
            {
                Logger.Info("Building Octopus Server Guest Authentication Provider v{0}", OctoVersionInfo.FullSemVer);

                // This is done to pass the data to github actions
                Console.Out.WriteLine($"::set-output name=semver::{OctoVersionInfo.FullSemVer}");
                Console.Out.WriteLine($"::set-output name=prerelease_tag::{OctoVersionInfo.PreReleaseTagWithDash}");

                DotNetBuild(_ => _
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .SetVersion(OctoVersionInfo.FullSemVer));
            });

    Target Pack =>
        _ => _
            .DependsOn(Compile)
            .Executes(() =>
            {
                Logger.Info("Packing Octopus Server Username Password Authentication Provider v{0}", OctoVersionInfo.FullSemVer);
                const string nuspecFile = "Octopus.Server.Extensibility.Authentication.Guest.nuspec";
                
                CopyFileToDirectory(BuildProjectDirectory / nuspecFile, PublishDirectory);
                CopyFileToDirectory(RootDirectory / "LICENSE.txt", PublishDirectory);
                CopyFileToDirectory(BuildProjectDirectory / "icon.png", PublishDirectory);
                CopyFileToDirectory(SourceDirectory / "Server" / "bin" / Configuration / "net5.0" / "Octopus.Server.Extensibility.Authentication.Guest.dll" , PublishDirectory);


                DotNetPack(_ => _
                    .SetProject(SourceDirectory / "Server"/ "Server.csproj")
                    .SetVersion(OctoVersionInfo.FullSemVer)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(ArtifactsDirectory)
                    .EnableNoBuild()
                    .DisableIncludeSymbols()
                    .SetVerbosity(DotNetVerbosity.Normal)
                    .SetProperty("NuspecFile", PublishDirectory / nuspecFile)
                    .SetProperty("NuspecProperties", $"Version={OctoVersionInfo.FullSemVer}"));
            
                DotNetPack(_ => _
                    .SetProject(SourceDirectory / "Client"/ "Client.csproj")
                    .SetVersion(OctoVersionInfo.FullSemVer)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(ArtifactsDirectory)
                    .EnableNoBuild()
                    .DisableIncludeSymbols()
                    .SetVerbosity(DotNetVerbosity.Normal));
            });

    Target CopyToLocalPackages =>
        _ => _
            .OnlyWhenStatic(() => IsLocalBuild)
            .TriggeredBy(Pack)
            .Executes(() =>
            {
                EnsureExistingDirectory(LocalPackagesDirectory);
                ArtifactsDirectory.GlobFiles("*.nupkg")
                    .ForEach(package =>
                    {
                        CopyFileToDirectory(package, LocalPackagesDirectory, FileExistsPolicy.Overwrite);
                    });
            });

    Target Default => _ => _
        .DependsOn(Pack)
        .DependsOn(CopyToLocalPackages);

    public static int Main() => Execute<Build>(x => x.Default);
}