$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
. "$toolsDir\helper.ps1"
$softwareName = $env:ChocolateyPackageName
$installDirectory = Get-PackageManagerInstallDirectory
$innoUninstaller = Join-Path $installDirectory 'unins000.exe'
$innoLog = Join-Path $env:TEMP ("{0}.uninstall.log" -f $env:ChocolateyPackageName)

[Environment]::SetEnvironmentVariable('INSTR_PKGMGR_UNINSTALL_MODE', 'SELF', 'Process')

if (-not (Test-Path $innoUninstaller)) {
    throw "The Inno uninstaller was not found at '$innoUninstaller'."
}

$packageArgs = @{
    packageName    = $env:ChocolateyPackageName
    softwareName   = $softwareName
    file           = $innoUninstaller
    fileType       = 'exe'
    silentArgs     = ('/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SELFUNINSTALL /LOG="{0}"' -f $innoLog)
    validExitCodes = @(0, 1605, 1614, 1641, 3010)
}

try {
    Uninstall-ChocolateyPackage @packageArgs
}
finally {
    [Environment]::SetEnvironmentVariable('INSTR_PKGMGR_UNINSTALL_MODE', $null, 'Process')
}

Uninstall-BinFile -Name (Get-PackageManagerCommandName)
Uninstall-BinFile -Name (Get-PackageManagerCliCommandName)
