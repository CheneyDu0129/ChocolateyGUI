#load nuget:?package=Chocolatey.Cake.Recipe&version=0.32.0

///////////////////////////////////////////////////////////////////////////////
// MODULES
///////////////////////////////////////////////////////////////////////////////
#module nuget:?package=Cake.Chocolatey.Module&version=0.3.0

///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////
#tool choco:?package=transifex-cli&version=1.6.5

if (BuildSystem.IsLocalBuild)
{
    Environment.SetVariableNames(
        gitReleaseManagerTokenVariable: "CHOCOLATEYGUI_GITHUB_PAT",
        transifexApiTokenVariable: "CHOCOLATEYGUI_TRANSIFEX_API_TOKEN"
    );
}
else
{
    Environment.SetVariableNames();
}

Func<FilePathCollection> getScriptsToVerify = () =>
{
    var scriptsToVerify = GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/**/*.{ps1|psm1|psd1}");

    Information("The following PowerShell scripts have been selected to be verified...");
    foreach (var scriptToVerify in scriptsToVerify)
    {
        Information(scriptToVerify.FullPath);
    }

    var numberOfScriptsToVerify = scriptsToVerify.Count();

    if (numberOfScriptsToVerify != 3)
    {
        throw new Exception(string.Format("Expected to find 3 scripts to verify, but found {0}", numberOfScriptsToVerify));
    }

    return scriptsToVerify;
};

Func<FilePathCollection> getScriptsToSign = () =>
{
    var scriptsToSign = GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/**/*.{ps1|psm1|psd1}");

    Information("The following PowerShell scripts have been selected to be signed...");
    foreach (var scriptToSign in scriptsToSign)
    {
        Information(scriptToSign.FullPath);
    }

    var numberOfScriptsToSign = scriptsToSign.Count();

    if (numberOfScriptsToSign != 3)
    {
        throw new Exception(string.Format("Expected to find 3 scripts to verify, but found {0}", numberOfScriptsToSign));
    }

    return scriptsToSign;
};

Func<FilePathCollection> getFilesToSign = () =>
{
    var filesToSign = GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/^{ChocolateyGui|ChocolateyGuiCli}/net48/{ChocolateyGui|ChocolateyGuiCli}*.{exe|dll}") +
                    GetFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/ChocolateyGui*/net48/ChocolateyGui*.dll");

    var platformTarget = ToolSettings.BuildPlatformTarget == PlatformTarget.MSIL ? "AnyCPU" : ToolSettings.BuildPlatformTarget.ToString();
    foreach(var project in ParseSolution(BuildParameters.SolutionFilePath).GetProjects())
    {
        var parsedProject = ParseProject(project.Path, BuildParameters.Configuration, platformTarget);
        if (parsedProject.RootNameSpace == "ChocolateyGui")
        {
            filesToSign.Add(parsedProject.OutputPaths.First().FullPath + "/ChocolateyGui.exe");
            continue;
        }

        if (parsedProject.RootNameSpace == "ChocolateyGuiCli")
        {
            filesToSign.Add(parsedProject.OutputPaths.First().FullPath + "/ChocolateyGuiCli.exe");
            continue;
        }

        if (parsedProject.RootNameSpace == "ChocolateyGui.Common")
        {
            filesToSign.Add(parsedProject.OutputPaths.First().FullPath + "/ChocolateyGui.Common.dll");
            continue;
        }

        if (parsedProject.RootNameSpace == "ChocolateyGui.Common.Windows")
        {
            filesToSign.Add(parsedProject.OutputPaths.First().FullPath + "/ChocolateyGui.Common.Windows.dll");
            continue;
        }
    }

    Information("The following assemblies have been selected to be signed...");
    foreach (var fileToSign in filesToSign)
    {
        Information(fileToSign.FullPath);
    }

    var numberOfFilesToSign = filesToSign.Count();

    if (numberOfFilesToSign != 13)
    {
        throw new Exception(string.Format("Expected to find 13 files to sign, but found {0}", numberOfFilesToSign));
    }

    return filesToSign;
};

Func<FilePathCollection> getMsisToSign = () =>
{
    var msisToSign = GetFiles(BuildParameters.Paths.Directories.Build + "/ChocolateyGUI.msi");

    Information("The following msi's have been selected to be signed...");
    foreach (var msiToSign in msisToSign)
    {
        Information(msiToSign.FullPath);
    }

    var numberOfMsisToSign = msisToSign.Count();

    if (numberOfMsisToSign != 1)
    {
        throw new Exception(string.Format("Expected to find 1 msis to sign, but found {0}", numberOfMsisToSign));
    }

    return msisToSign;
};

var companyProfile = Argument("companyProfile", "semight");
var packagePrefix = Argument("packagePrefix", string.Empty);
var packageVersionOverride = Argument("packageVersion", "1.0.0");
var useBranchPackageVersion = Argument("useBranchPackageVersion", false);

var supportedCompanyProfiles = new[] { "semight", "nexustest" };
if (!supportedCompanyProfiles.Contains(companyProfile, StringComparer.OrdinalIgnoreCase))
{
    throw new Exception(string.Format("Unsupported companyProfile '{0}'. Allowed values: semight, nexustest.", companyProfile));
}

var companyName = "Semight Instrument";
if (companyProfile.Equals("nexustest", StringComparison.OrdinalIgnoreCase))
{
    companyName = "Nexustest";
}

var packageDisplayName = "Package Manager";
var packageId = string.IsNullOrWhiteSpace(packagePrefix)
    ? "instr-pkgmgr"
    : string.Format("{0}-instr-pkgmgr", packagePrefix.ToLowerInvariant());
var releaseNotesSourcePath = MakeAbsolute(File("./CHANGELOG.md")).FullPath;
var packageCopyright = string.Format("Copyright 2014 - Present Open Source maintainers and {0}.", companyName);

Func<string, string> getReleaseNotesContent = targetVersion =>
{
    if (!System.IO.File.Exists(releaseNotesSourcePath))
    {
        return "Release notes are maintained by the team in CHANGELOG.md.";
    }

    var changelogContent = System.IO.File.ReadAllText(releaseNotesSourcePath).Trim();
    if (string.IsNullOrWhiteSpace(changelogContent))
    {
        return "Release notes are maintained by the team in CHANGELOG.md.";
    }

    string resolvedReleaseNotes = null;
    if (!string.IsNullOrWhiteSpace(targetVersion))
    {
        var targetCoreVersion = targetVersion.Split('-')[0];
        var headingRegex = new System.Text.RegularExpressions.Regex(@"^##\s*\[?(?<version>[^\]\s]+)\]?[^\r\n]*$", System.Text.RegularExpressions.RegexOptions.Multiline);
        var headingMatches = headingRegex.Matches(changelogContent);

        for (var i = 0; i < headingMatches.Count; i++)
        {
            var currentMatch = headingMatches[i];
            var candidateVersion = currentMatch.Groups["version"].Value;
            var candidateCoreVersion = candidateVersion.Split('-')[0];
            var isMatch = string.Equals(candidateVersion, targetVersion, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidateCoreVersion, targetCoreVersion, StringComparison.OrdinalIgnoreCase)
                || targetVersion.StartsWith(candidateVersion, StringComparison.OrdinalIgnoreCase)
                || candidateVersion.StartsWith(targetVersion, StringComparison.OrdinalIgnoreCase);

            if (!isMatch)
            {
                continue;
            }

            var sectionStart = currentMatch.Index;
            var sectionEnd = i + 1 < headingMatches.Count ? headingMatches[i + 1].Index : changelogContent.Length;
            resolvedReleaseNotes = changelogContent.Substring(sectionStart, sectionEnd - sectionStart).Trim();
            Information("Release notes section selected for version: {0}", candidateVersion);
            break;
        }
    }

    if (string.IsNullOrWhiteSpace(resolvedReleaseNotes))
    {
        resolvedReleaseNotes = changelogContent;
        Information("Release notes section not found for version '{0}', using full CHANGELOG content.", targetVersion ?? "(null)");
    }

    // Nuspec releaseNotes is xml text, so escape special chars before replacement.
    return System.Security.SecurityElement.Escape(resolvedReleaseNotes);
};

Information("Branding profile: {0}", companyProfile);
Information("Computed package id: {0}", packageId);
Information("Release notes source: {0}", releaseNotesSourcePath);

Action<string, string> setBuildVersionProperty = (propertyName, value) =>
{
    var target = BuildParameters.Version;
    if (target == null || string.IsNullOrWhiteSpace(value))
    {
        return;
    }

    var property = typeof(BuildVersion).GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
    if (property == null)
    {
        return;
    }

    var setter = property.GetSetMethod(true);
    if (setter == null)
    {
        return;
    }

    setter.Invoke(target, new object[] { value });
};

Func<string> resolvePackageVersion = () =>
{
    if (!string.IsNullOrWhiteSpace(packageVersionOverride))
    {
        return packageVersionOverride.Trim();
    }

    if (useBranchPackageVersion)
    {
        return BuildParameters.Version?.PackageVersion;
    }

    return BuildParameters.Version?.MajorMinorPatch;
};

string originalPackageVersionForChocolatey = null;

TaskSetup(taskSetupContext =>
{
    if (!string.Equals(taskSetupContext.Task.Name, "Create-Chocolatey-Packages", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var effectivePackageVersion = resolvePackageVersion();
    if (originalPackageVersionForChocolatey == null)
    {
        originalPackageVersionForChocolatey = BuildParameters.Version?.PackageVersion;
    }

    if (!string.IsNullOrWhiteSpace(effectivePackageVersion)
        && !string.Equals(BuildParameters.Version?.PackageVersion, effectivePackageVersion, StringComparison.OrdinalIgnoreCase))
    {
        Information("Overriding package version from '{0}' to '{1}'.", BuildParameters.Version?.PackageVersion, effectivePackageVersion);
        setBuildVersionProperty("PackageVersion", effectivePackageVersion);
    }

    var nuspecFiles = GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + BuildParameters.ChocolateyNuspecGlobbingPattern);
    foreach (var nuspecFile in nuspecFiles)
    {
        var nuspecContent = System.IO.File.ReadAllText(nuspecFile.FullPath);
        nuspecContent = nuspecContent
            .Replace("__PACKAGE_ID__", packageId)
            .Replace("__PACKAGE_TITLE__", packageDisplayName)
            .Replace("__PACKAGE_AUTHORS__", companyName)
            .Replace("__PACKAGE_OWNERS__", companyName)
            .Replace("__PACKAGE_COPYRIGHT__", packageCopyright)
            .Replace("__RELEASE_NOTES__", getReleaseNotesContent(BuildParameters.Version.PackageVersion));
        System.IO.File.WriteAllText(nuspecFile.FullPath, nuspecContent);
    }

    var creditsMarkdownPath = MakeAbsolute(File("./CREDITS.md")).FullPath;
    if (System.IO.File.Exists(creditsMarkdownPath))
    {
        var creditsMarkdownContent = System.IO.File.ReadAllText(creditsMarkdownPath);
        creditsMarkdownContent = creditsMarkdownContent
            .Replace("__PRODUCT_NAME__", packageDisplayName);
        System.IO.File.WriteAllText(creditsMarkdownPath, creditsMarkdownContent);
    }

    var creditsJsonPath = MakeAbsolute(File("./CREDITS.json")).FullPath;
    if (System.IO.File.Exists(creditsJsonPath))
    {
        var creditsJsonContent = System.IO.File.ReadAllText(creditsJsonPath);
        creditsJsonContent = creditsJsonContent
            .Replace("__PRODUCT_NAME__", packageDisplayName);
        System.IO.File.WriteAllText(creditsJsonPath, creditsJsonContent);
    }

    var installScriptPath = BuildParameters.Paths.Directories.ChocolateyNuspecDirectory.CombineWithFilePath("chocolateyInstall.ps1").FullPath;
    if (System.IO.File.Exists(installScriptPath))
    {
        var installScriptContent = System.IO.File.ReadAllText(installScriptPath);
        installScriptContent = installScriptContent.Replace("$env:ChocolateyPackageName", string.Format("'{0}'", packageDisplayName));
        System.IO.File.WriteAllText(installScriptPath, installScriptContent);
    }
});

TaskTeardown(taskTeardownContext =>
{
    if (!string.Equals(taskTeardownContext.Task.Name, "Create-Chocolatey-Packages", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(originalPackageVersionForChocolatey))
    {
        return;
    }

    if (string.Equals(BuildParameters.Version?.PackageVersion, originalPackageVersionForChocolatey, StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    Information("Restoring package version back to '{0}' for subsequent packaging tasks.", originalPackageVersionForChocolatey);
    setBuildVersionProperty("PackageVersion", originalPackageVersionForChocolatey);
});

BuildParameters.SetParameters(context: Context,
                            buildSystem: BuildSystem,
                            sourceDirectoryPath: "./Source",
                            solutionFilePath: "./Source/ChocolateyGui.sln",
                            solutionDirectoryPath: "./Source/ChocolateyGui",
                            resharperSettingsFileName: "ChocolateyGui.sln.DotSettings",
                            title: packageDisplayName,
                            repositoryOwner: "chocolatey",
                            repositoryName: "ChocolateyGUI",
                            shouldDownloadMilestoneReleaseNotes: true,
                            treatWarningsAsErrors: false,
                            productName: packageDisplayName,
                            productDescription: string.Format("{0} - All Rights Reserved", packageDisplayName),
                            productCopyright: string.Format("Copyright 2014 - Present Open Source maintainers and {0} - All Rights Reserved.", companyName),
                            useChocolateyGuiStrongNameKey: true,
                            getScriptsToVerify: getScriptsToVerify,
                            getScriptsToSign: getScriptsToSign,
                            getFilesToSign: getFilesToSign,
                            getMsisToSign: getMsisToSign,
                            shouldBuildMsi: true,
                            strongNameDependentAssembliesInputPath: string.Format("{0}{1}", ((FilePath)("./Source")).FullPath, "\\packages\\Splat*"),
                            shouldRunInspectCode: false);

ToolSettings.SetToolSettings(context: Context);

BuildParameters.PrintParameters(Context);

Build.RunDotNet();
