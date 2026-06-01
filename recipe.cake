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
    var msisToSign = GetFiles(BuildParameters.Paths.Directories.Build + "/ChocolateyGUI.exe");

    Information("The following installer binaries have been selected to be signed...");
    foreach (var msiToSign in msisToSign)
    {
        Information(msiToSign.FullPath);
    }

    var numberOfMsisToSign = msisToSign.Count();

    if (numberOfMsisToSign != 1)
    {
        throw new Exception(string.Format("Expected to find 1 installer binary to sign, but found {0}", numberOfMsisToSign));
    }

    return msisToSign;
};

var companyProfile = Argument("companyProfile", "semight");
var packagePrefix = Argument("packagePrefix", string.Empty);
var oemIdOverride = Argument("oemId", string.Empty);
var oemNameOverride = Argument("oemName", string.Empty);
var appNameOverride = Argument("appName", string.Empty);
var packageVersionOverride = Argument("packageVersion", string.Empty);

var unifiedVersionFilePath = MakeAbsolute(File("./VERSION.txt")).FullPath;
Func<string> resolveUnifiedVersionFromFile = () =>
{
    if (!System.IO.File.Exists(unifiedVersionFilePath))
    {
        throw new Exception(string.Format("Unified version file not found: {0}", unifiedVersionFilePath));
    }

    var configuredVersion = System.IO.File.ReadAllText(unifiedVersionFilePath).Trim();
    if (string.IsNullOrWhiteSpace(configuredVersion))
    {
        throw new Exception(string.Format("Unified version file is empty: {0}", unifiedVersionFilePath));
    }

    return configuredVersion;
};

var unifiedVersion = resolveUnifiedVersionFromFile();

Func<string, string> normalizePackageToken = value =>
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    var normalized = new System.Text.StringBuilder();
    foreach (var character in value.Trim().ToLowerInvariant())
    {
        if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9'))
        {
            normalized.Append(character);
            continue;
        }

        if (character == '-' || character == '_' || character == ' ')
        {
            normalized.Append('-');
        }
    }

    return normalized.ToString().Trim('-');
};

Func<string, string> toDirectoryToken = value =>
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    var result = new System.Text.StringBuilder();
    var makeUpper = true;
    foreach (var character in value.Trim())
    {
        if ((character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9'))
        {
            result.Append(makeUpper ? char.ToUpperInvariant(character) : character);
            makeUpper = false;
            continue;
        }

        makeUpper = true;
    }

    return result.ToString();
};

Func<string, string> createDeterministicInnoAppId = value =>
{
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new Exception("Cannot generate an Inno AppId from an empty identity value.");
    }

    using (var md5 = System.Security.Cryptography.MD5.Create())
    {
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("ChocolateyGUI.PackageManager.OEM:" + value.ToLowerInvariant()));
        var guid = new Guid(bytes).ToString().ToUpperInvariant();
        return "{{" + guid + "}";
    }
};

var supportedCompanyProfiles = new[] { "semight", "nexustest" };
if (!supportedCompanyProfiles.Contains(companyProfile, StringComparer.OrdinalIgnoreCase))
{
    throw new Exception(string.Format("Unsupported companyProfile '{0}'. Allowed values: semight, nexustest.", companyProfile));
}

var productDirectoryName = "PackageManager";
var effectivePackagePrefix = normalizePackageToken(packagePrefix);
if (string.IsNullOrWhiteSpace(effectivePackagePrefix) && !companyProfile.Equals("semight", StringComparison.OrdinalIgnoreCase))
{
    effectivePackagePrefix = normalizePackageToken(companyProfile);
}

if (!string.IsNullOrWhiteSpace(packagePrefix) && string.IsNullOrWhiteSpace(effectivePackagePrefix))
{
    throw new Exception(string.Format("packagePrefix '{0}' does not contain any valid package id characters.", packagePrefix));
}

var oemDirectoryName = "Semight Instruments";
if (!string.IsNullOrWhiteSpace(oemIdOverride))
{
    oemDirectoryName = oemIdOverride.Trim();
}
else if (!string.IsNullOrWhiteSpace(effectivePackagePrefix))
{
    oemDirectoryName = toDirectoryToken(effectivePackagePrefix);
}
else if (companyProfile.Equals("nexustest", StringComparison.OrdinalIgnoreCase))
{
    oemDirectoryName = "Nexustest";
}

if (string.IsNullOrWhiteSpace(oemDirectoryName))
{
    throw new Exception("OEM directory name cannot be empty after normalization.");
}

var companyName = !string.IsNullOrWhiteSpace(oemNameOverride)
    ? oemNameOverride.Trim()
    : (oemDirectoryName.Equals("Semight Instruments", StringComparison.OrdinalIgnoreCase) ? "Semight Instruments" : oemDirectoryName);
var defaultDisplayBrandName = string.IsNullOrWhiteSpace(effectivePackagePrefix)
    && string.IsNullOrWhiteSpace(oemNameOverride)
    && companyProfile.Equals("semight", StringComparison.OrdinalIgnoreCase)
    ? "Semight"
    : companyName;
var packageDisplayName = !string.IsNullOrWhiteSpace(appNameOverride)
    ? appNameOverride.Trim()
    : string.Format("{0} Package Manager", defaultDisplayBrandName);
var packageId = string.IsNullOrWhiteSpace(effectivePackagePrefix)
    ? "instr-pkgmgr"
    : string.Format("{0}-instr-pkgmgr", effectivePackagePrefix);
var installerOutputBaseName = packageId;
var installerFileName = string.Format("{0}.exe", installerOutputBaseName);
var packageInstallDirectory = string.Format("{0}\\{1}", oemDirectoryName, productDirectoryName);
var packageCommandName = packageId;
var packageCliCommandName = string.Format("{0}-cli", packageId);
var packageGuiExeName = string.Format("{0}.exe", packageCommandName);
var packageCliExeName = string.Format("{0}.exe", packageCliCommandName);
var packageGuiMutexName = string.Format("Global\\{0}-mutex", packageCommandName);
var packageCliMutexName = string.Format("Global\\{0}-mutex", packageCliCommandName);
var providedWhitelistPackageId = string.IsNullOrWhiteSpace(effectivePackagePrefix)
    ? "instr-whitelist-package"
    : string.Format("{0}-instr-whitelist-package", effectivePackagePrefix);
var offlineSourceName = "PackageOfflineSource";
var innoAppId = string.IsNullOrWhiteSpace(effectivePackagePrefix) && oemDirectoryName.Equals("Semight Instruments", StringComparison.OrdinalIgnoreCase)
    ? "{{2F8DCB25-5F34-4C8B-9F4A-3F3FC895F9A2}"
    : createDeterministicInnoAppId(packageId);
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
        var headingRegex = new System.Text.RegularExpressions.Regex(@"^##\s*\[?(?<version>[^\]\s]+)\]?[\r\n]*$", System.Text.RegularExpressions.RegexOptions.Multiline);
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
Information("OEM directory name: {0}", oemDirectoryName);
Information("Computed package id: {0}", packageId);
Information("Computed GUI command: {0}", packageCommandName);
Information("Computed CLI command: {0}", packageCliCommandName);
Information("Computed Inno AppId: {0}", innoAppId);
Information("Release notes source: {0}", releaseNotesSourcePath);
Information("Unified version source: {0}", unifiedVersionFilePath);
Information("Unified version value: {0}", unifiedVersion);

var stableAssemblyVersion = "0.1.0.0";

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

Action<string, string> regenerateSolutionVersionFile = (fileVersion, informationalVersion) =>
{
    var assemblyInfoSettings = new AssemblyInfoSettings {
        ComVisible = BuildParameters.ProductComVisible,
        CLSCompliant = BuildParameters.ProductClsCompliant,
        Company = BuildParameters.ProductCompany,
        Version = stableAssemblyVersion,
        FileVersion = fileVersion,
        InformationalVersion = informationalVersion,
        Product = BuildParameters.ProductName,
        Description = BuildParameters.ProductDescription,
        Trademark = BuildParameters.ProductTrademark,
        Copyright = BuildParameters.ProductCopyright
    };

    if (BuildParameters.ProductCustomAttributes != null)
    {
        assemblyInfoSettings.CustomAttributes = BuildParameters.ProductCustomAttributes;
    }

    CreateAssemblyInfo(BuildParameters.Paths.Files.SolutionInfoFilePath, assemblyInfoSettings);
};

Func<string, string> normalizePackageVersion = version =>
{
    if (string.IsNullOrWhiteSpace(version))
    {
        return version;
    }

    var normalizedVersion = version.Trim();
    while (normalizedVersion.EndsWith("-", StringComparison.Ordinal))
    {
        normalizedVersion = normalizedVersion.Substring(0, normalizedVersion.Length - 1);
    }

    return normalizedVersion;
};

Func<string, string> toFourPartAssemblyVersion = version =>
{
    var normalizedVersion = normalizePackageVersion(version);
    if (string.IsNullOrWhiteSpace(normalizedVersion))
    {
        throw new Exception("Unified version cannot be empty when converting to assembly version.");
    }

    var coreVersion = normalizedVersion.Split('-')[0];
    var segments = coreVersion.Split('.');

    if (segments.Length == 3)
    {
        return string.Format("{0}.0", coreVersion);
    }

    if (segments.Length == 4)
    {
        return coreVersion;
    }

    throw new Exception(string.Format("Unified version '{0}' must have 3 or 4 numeric segments for assembly version.", normalizedVersion));
};

Func<string> resolvePackageVersion = () =>
{
    if (!string.IsNullOrWhiteSpace(packageVersionOverride))
    {
        return packageVersionOverride.Trim();
    }

    return unifiedVersion;
};

var hasAppliedStableVersionMetadata = false;

TaskSetup(taskSetupContext =>
{
    if (!hasAppliedStableVersionMetadata
        && !string.Equals(taskSetupContext.Task.Name, "Setup", StringComparison.OrdinalIgnoreCase))
    {
        var stableVersion = resolvePackageVersion();
        if (!string.IsNullOrWhiteSpace(stableVersion))
        {
            setBuildVersionProperty("InformationalVersion", stableVersion);
            setBuildVersionProperty("PackageVersion", stableVersion);

            regenerateSolutionVersionFile(toFourPartAssemblyVersion(stableVersion), stableVersion);
        }

        hasAppliedStableVersionMetadata = true;
    }

    if (!string.Equals(taskSetupContext.Task.Name, "Create-Chocolatey-Packages", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var effectivePackageVersion = resolvePackageVersion();
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
            .Replace("__WHITELIST_PACKAGE_ID__", providedWhitelistPackageId)
            .Replace("__INSTALLER_FILE_NAME__", installerFileName)
            .Replace("__PACKAGE_TAGS__", string.Format("{0} chocolatey admin foss {1}", packageId, oemDirectoryName.ToLowerInvariant()))
            .Replace("__RELEASE_NOTES__", getReleaseNotesContent(BuildParameters.Version.PackageVersion));
        System.IO.File.WriteAllText(nuspecFile.FullPath, nuspecContent);
    }

    var helperPath = MakeAbsolute(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory.CombineWithFilePath("helper.ps1")).FullPath;
    if (System.IO.File.Exists(helperPath))
    {
        var helperContent = System.IO.File.ReadAllText(helperPath);
        helperContent = helperContent
            .Replace("__PACKAGE_INSTALL_DIRECTORY__", packageInstallDirectory)
            .Replace("__PACKAGE_GUI_EXE_NAME__", packageGuiExeName)
            .Replace("__PACKAGE_CLI_EXE_NAME__", packageCliExeName)
            .Replace("__PACKAGE_COMMAND_NAME__", packageCommandName)
            .Replace("__PACKAGE_CLI_COMMAND_NAME__", packageCliCommandName);
        System.IO.File.WriteAllText(helperPath, helperContent);
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
                            shouldBuildMsi: false,
                            shouldVerifyPowerShellScripts: false,
                            strongNameDependentAssembliesInputPath: string.Format("{0}{1}", ((FilePath)("./Source")).FullPath, "\\packages\\Splat*"),
                            shouldRunInspectCode: false,
                            shouldRunGitVersion: false);

var innoCompilerPath = Argument("innoCompilerPath", @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe");
var innoScriptPath = MakeAbsolute(File("./Source/ChocolateyGui.Install/ChocolateyGUI.iss")).FullPath;

Task("Build-Inno-Installer")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping due to not running on Windows")
    .IsDependentOn("DotNetBuild")
    .IsDependentOn("Sign-Assemblies")
    .Does(() =>
{
    if (!System.IO.File.Exists(innoCompilerPath))
    {
        throw new Exception(string.Format("Inno Setup compiler not found at '{0}'. Install Inno Setup 6 or pass --innoCompilerPath=...", innoCompilerPath));
    }

    var guiTargetDir = MakeAbsolute(BuildParameters.Paths.Directories.PublishedApplications.Combine("ChocolateyGui").Combine("net48")).FullPath;
    var guiCliTargetDir = MakeAbsolute(BuildParameters.Paths.Directories.PublishedApplications.Combine("ChocolateyGuiCli").Combine("net48")).FullPath;
    var cleanupScriptPath = MakeAbsolute(File("./Source/ChocolateyGui.Install/CleanupProvidedPackages.ps1")).FullPath;
    var outputDir = MakeAbsolute(BuildParameters.Paths.Directories.Build).FullPath;
    var resolvedCleanupScriptPath = System.IO.Path.Combine(outputDir, "CleanupProvidedPackages.ps1");
    var brandingConfigPath = System.IO.Path.Combine(outputDir, "branding.config");

    var guiExePath = System.IO.Path.Combine(guiTargetDir, "ChocolateyGui.exe");
    var cliExePath = System.IO.Path.Combine(guiCliTargetDir, "ChocolateyGuiCli.exe");

    if (!System.IO.File.Exists(guiExePath) || !System.IO.File.Exists(cliExePath))
    {
        throw new Exception("Expected GUI and CLI binaries were not found. Ensure DotNetBuild completed successfully before Build-Inno-Installer.");
    }

    if (!System.IO.File.Exists(cleanupScriptPath))
    {
        throw new Exception(string.Format("Cleanup script not found: {0}", cleanupScriptPath));
    }

    EnsureDirectoryExists(outputDir);

    var cleanupScriptContent = System.IO.File.ReadAllText(cleanupScriptPath)
        .Replace("__PACKAGE_ID__", packageId)
        .Replace("__PACKAGE_COMMAND_NAME__", packageCommandName)
        .Replace("__PACKAGE_CLI_COMMAND_NAME__", packageCliCommandName)
        .Replace("__OEM_DIRECTORY_NAME__", oemDirectoryName)
        .Replace("__PRODUCT_DIRECTORY_NAME__", productDirectoryName)
        .Replace("__OFFLINE_SOURCE_NAME__", offlineSourceName)
        .Replace("__WHITELIST_PACKAGE_ID__", providedWhitelistPackageId);
    System.IO.File.WriteAllText(resolvedCleanupScriptPath, cleanupScriptContent);

    var brandingConfigContent = string.Join(System.Environment.NewLine, new[] {
        string.Format("CompanyDirectoryName={0}", oemDirectoryName),
        string.Format("ProductDirectoryName={0}", productDirectoryName),
        string.Format("PackageId={0}", packageId),
        string.Format("AppMutexName={0}", packageGuiMutexName),
        string.Format("CliMutexName={0}", packageCliMutexName)
    });
    System.IO.File.WriteAllText(brandingConfigPath, brandingConfigContent);

    var arguments = string.Format(
        "/Qp /DMyAppVersion=\"{0}\" /DMyAppName=\"{1}\" /DMyAppPublisher=\"{2}\" /DCompanyDirectoryName=\"{3}\" /DProductDirectoryName=\"{4}\" /DAppId=\"{5}\" /DGuiTargetDir=\"{6}\" /DGuiCliTargetDir=\"{7}\" /DCleanupScriptPath=\"{8}\" /DBrandingConfigPath=\"{9}\" /DOutputDir=\"{10}\" /DOutputBaseFilename=\"{11}\" /DMyAppExeName=\"{12}\" /DMyCliExeName=\"{13}\" /DMyAppMutexName=\"{14}\" /DMyCliMutexName=\"{15}\" \"{16}\"",
        BuildParameters.Version.PackageVersion,
        packageDisplayName,
        companyName,
        oemDirectoryName,
        productDirectoryName,
        innoAppId,
        guiTargetDir,
        guiCliTargetDir,
        resolvedCleanupScriptPath,
        brandingConfigPath,
        outputDir,
        installerOutputBaseName,
        packageGuiExeName,
        packageCliExeName,
        packageGuiMutexName,
        packageCliMutexName,
        innoScriptPath
    );

    var exitCode = StartProcess(innoCompilerPath, new ProcessSettings {
        Arguments = arguments
    });

    if (exitCode != 0)
    {
        throw new Exception(string.Format("Inno Setup compilation failed with exit code {0}", exitCode));
    }

    var installerPath = MakeAbsolute(File("./code_drop/" + installerFileName));
    if (!FileExists(installerPath))
    {
        throw new Exception(string.Format("Expected installer output was not found: {0}", installerPath));
    }

    BuildParameters.BuildProvider.UploadArtifact(installerPath);
});

BuildParameters.Tasks.SignMsisTask.IsDependentOn("Build-Inno-Installer");

ToolSettings.SetToolSettings(context: Context);

BuildParameters.PrintParameters(Context);

Build.RunDotNet();
