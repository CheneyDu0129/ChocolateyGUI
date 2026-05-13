# Package Manager

This repository contains a company-branded package manager distribution based on upstream Chocolatey GUI.
It is intended for customized redistribution and internal or commercial deployment by the current package publisher.

## Installation

The default package id produced by this fork is `instr-pkgmgr`.
Install the published package provided by your package publisher or internal repository:

```powershell
choco install instr-pkgmgr
```

If your organization uses a branded package prefix, install the published package id that matches that branding profile.

## Support

For support, documentation, and issue reporting, use the support channel operated by the publisher of this distribution.
Do not report issues for this fork to upstream Chocolatey community channels unless the problem is confirmed to exist in the upstream project itself.

## Information

* Upstream project history and attribution are retained in [ABOUT.md](ABOUT.md).
* Contribution expectations for this fork are described in [CONTRIBUTING.md](CONTRIBUTING.md).
* Third-party licenses and redistribution notices are listed in [CREDITS.md](CREDITS.md).

### Documentation

Publisher-specific documentation should be distributed together with the package or maintained in the repository that publishes this fork.
Upstream Chocolatey GUI documentation can still be useful for historical reference, but it does not describe support obligations for this distribution.

### Requirements

* .NET Framework 4.8
* Should work on all Windows Operating Systems from Windows 7 SP1 and above, and Windows Server 2008 R2 SP1 and above

### License / Credits

This distribution includes upstream open source components and local modifications.
See [LICENSE.txt](LICENSE.txt), [NOTICE](NOTICE), and [CREDITS.md](CREDITS.md) for the applicable license and attribution information.

## Submitting Issues

If you have found an issue in this distribution, report it to the current package publisher or the repository that distributes this fork.

Observe the following help for submitting an issue:

Prerequisites:

* The issue has to do with this distribution itself and is not just a packaging or environment-specific deployment issue.
* Please check to see if your issue already exists with a quick search of the issues. Start with one relevant term and then add if you get too many results.
* Enhancements and code contributions should observe the guidance in [CONTRIBUTING.md](CONTRIBUTING.md).

Submitting a ticket:

* We'll need debug and verbose output, so please run and capture the log with `-dv` or `--debug --verbose`. You can submit that with the issue or create a gist and link it.
* **Please note** that the debug/verbose output for some commands may contain sensitive data such as passwords or apiKeys, so remove those before sharing logs.
* choco.exe logs to a file in `$env:ChocolateyInstall\log\`. You can grab the specific log output from there so you don't have to capture or redirect screen output. Please limit the amount included to just the command run (the log is appended to with every command).
* Save the log output in the issue system accepted by your publisher. If you use a gist or similar sharing mechanism, make sure the log does not contain secrets.
* We'll need the entire log output from the run, so please don't limit it down to areas you feel are relevant. You may miss some important details we'll need to know. This will help expedite issue triage.
* It's helpful to include the version of choco, the version of the OS, and the version of PowerShell (Posh), but the debug script should capture all of those pieces of information.
* Include screenshots and/or animated gif's whenever possible, they help show us exactly what the problem is.

## Contributing

If you would like to contribute code or help fix bugs in this fork, please review [CONTRIBUTING.md](CONTRIBUTING.md) for the current maintainer workflow.

### Building

* It is assumed that a version of Visual Studio 2019 or newer is already installed on the machine being used to complete the build.
* `choco install wixtoolset -y`
* **Add By DU JIAWEI**: `Install-Module -Name ConvertToSARIF -RequiredVersion 1.0.0 -SkipPublisherCheck -Force -Scope CurrentUser`
* **OPTIONAL:** Set `FXCOPDIR` environment variable, which can be set using [vswhere](https://chocolatey.org/packages/vswhere) and the following command:

   ```ps1
   $FXCOPDIR = vswhere -products * -latest -prerelease -find **/FxCopCmd.exe
   [Environment]::SetEnvironmentVariable("FXCOPDIR", $FXCOPDIR, 'User')
   refreshenv
   ```

* Install WiX toolset integration for Visual Studio from [WiX Toolset Visual Studio 2019 Extension](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2019Extension)
* From an **Administrative** PowerShell Window, navigate to the folder where you have cloned this repository and run `build.ps1`, this will run Cake and it will go through the build script.

  ```ps1
  ./build.ps1
  ```

#### Branding Parameters

The build now supports company branding profiles through Cake arguments.

* Default profile is `semight` (no extra arguments required).
* Supported `companyProfile` values: `semight`, `nexustest`.
* Package id defaults to `instr-pkgmgr`.
* Optional `packagePrefix` can be used for future id split (for example: `semight-instr-pkgmgr`).
* Chocolatey package (`instr-pkgmgr`) defaults to version `1.0.0`.
* Optional `packageVersion` sets an explicit Chocolatey package version (recommended release form: `./build.ps1 --companyProfile=semight --packageVersion=xxx`).
* Optional `useBranchPackageVersion=true` uses the branch-derived prerelease version for the Chocolatey package.
* NuGet packages (`ChocolateyGui.Common*`) continue to use the GitVersion-calculated version.

#### Company Rename Checklist

If you need to change the company name (for example, from `Semight Instruments` to another organization), use the checklist below to avoid path or packaging mismatches.

1. Runtime path constants (single source of truth)
  - Update `CompanyDirectoryName` (and `ProductDirectoryName` when needed) in `Source/ChocolateyGui.Common/Constants/BrandingConstants.cs`.
  - This controls runtime `ProgramData` and `LocalAppData` path segments used by both GUI and CLI.

2. MSI installer directory variables
  - Update `CompanyDirectoryName` and `ProductDirectoryName` in `Source/ChocolateyGui.Install/Product.wxs`.
  - This controls installer-created folders under:
    - `C:\ProgramData\<CompanyDirectoryName>\<ProductDirectoryName>\Config`
    - `%LOCALAPPDATA%\<CompanyDirectoryName>\<ProductDirectoryName>`
    - `C:\<CompanyDirectoryName>\<ProductDirectoryName>` (current install root strategy)

3. Product display metadata (if brand text changes)
  - Update `ProductName` and `Manufacturer` in `Source/ChocolateyGui.Install/Product.wxs`.
  - Update assembly metadata in `Source/SolutionVersion.cs` when required.

4. Packaging/company profile metadata
  - Check company profile mapping in `recipe.cake` (`companyProfile`, `companyName`, `packageDisplayName`, `packageId`).
  - Build with the target profile and verify generated package id/title.

5. Documentation and legal artifacts
  - Review `README.md`, `ABOUT.md`, `CONTRIBUTING.md`, `CREDITS.md`, and `CREDITS.json` for visible company naming.

6. Final validation after rename
  - Run `./build.ps1 --companyProfile=<target>`.
  - Verify installed paths, ARP display name/manufacturer, and CLI shim command names.

Examples:

```ps1
./build.ps1 --companyProfile=semight
./build.ps1 --companyProfile=nexustest
./build.ps1 --companyProfile=semight --packagePrefix=semight
./build.ps1 --companyProfile=semight --packageVersion=xxx
./build.ps1 --companyProfile=semight --useBranchPackageVersion=true
```

#### Version and Release Notes Ownership

* Version rules are maintained in `GitVersion.yml`.
* Release notes are maintained by the team in `CHANGELOG.md`.
* Package metadata `releaseNotes` is injected from `CHANGELOG.md` during packaging.
* `chocolateyInstall.ps1` package display name is profile-driven during packaging (semight/nexustest) without changing source script signatures.

### Localization

If you are interested in localizing this distribution, follow the localization workflow defined by the current maintainer team.

## Committers

Committers should follow the maintainer guidance and release process defined for this fork.

## Features

* View all **installed** and **available** packages
* **Update** installed but outdated packages
* **Install** and **uninstall** packages
* See detailed **package information**

## Credits

This distribution depends on a number of upstream projects and libraries. See [CREDITS.md](CREDITS.md) for the full attribution list.
