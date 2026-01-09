using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    /* Reminder copied from the Nuke FAQ
     
     While MSBuild is a powerful build system and definitely worth exploring for some use-cases, it falls rather short
     when it comes to general build automation. The XML format makes it very verbose even for medium complex logic.
     
     Writing custom tasks in C# is a possible but not practical due to the maintenance costs and cumbersome debugging.
     NUKE doesn't replace MSBuild though. Using the DotNetTasks and MSBuildTasks, it is still used to compile
     solutions and projects.

     Our rule of thumb: when something is closely related to the compilation process and involved files, you should use
     MSBuild. When it's not, like packaging the output files, you could use NUKE.
     */

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    Target Gircore => _ => _
        .Before(Compile)
        .Description("Generating libraries for Gir.Core")
        .Executes(() =>
        {
            var workingDir = $"{RootDirectory}/lib/gircore/scripts";
            var process = ProcessTasks.StartProcess("dotnet", $"fsi GenerateLibs.fsx", workingDir, null, null, true);
            process.WaitForExit();
        });

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .DependsOn(Gircore)
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
        });
}