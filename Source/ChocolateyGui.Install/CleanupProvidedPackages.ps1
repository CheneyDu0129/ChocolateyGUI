param(
    [ValidateSet('Self', 'Purge')]
    [string] $Mode = 'Self'
)

$ErrorActionPreference = 'Stop'

function Test-UninstallDebugEnabled {
    $rawValue = [Environment]::GetEnvironmentVariable('INSTR_PKGMGR_UNINSTALL_DEBUG', 'Process')
    if ([string]::IsNullOrWhiteSpace($rawValue)) {
        $rawValue = [Environment]::GetEnvironmentVariable('INSTR_PKGMGR_UNINSTALL_DEBUG', 'User')
    }
    if ([string]::IsNullOrWhiteSpace($rawValue)) {
        $rawValue = [Environment]::GetEnvironmentVariable('INSTR_PKGMGR_UNINSTALL_DEBUG', 'Machine')
    }

    if ([string]::IsNullOrWhiteSpace($rawValue)) {
        return $false
    }

    switch ($rawValue.Trim().ToUpperInvariant()) {
        '1' { return $true }
        'TRUE' { return $true }
        'YES' { return $true }
        'ON' { return $true }
        default { return $false }
    }
}

function Write-CleanupLog {
    param(
        [string] $Level,
        [string] $Message
    )

    if ([string]::IsNullOrWhiteSpace($script:CleanupLogPath) -or [string]::IsNullOrWhiteSpace($Message)) {
        return
    }

    $normalizedLevel = if ([string]::IsNullOrWhiteSpace($Level)) { 'INFO' } else { $Level }
    $line = '[{0}] [{1}] {2}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'), $normalizedLevel, $Message

    try {
        Add-Content -LiteralPath $script:CleanupLogPath -Value $line -Encoding UTF8
    }
    catch {
        # Best-effort logging only.
    }
}

$script:IsUninstallDebugEnabled = Test-UninstallDebugEnabled
$script:CleanupLogPath = $null

if ($script:IsUninstallDebugEnabled) {
    $script:CleanupLogPath = Join-Path $env:TEMP '__PACKAGE_ID__.cleanup-provided-packages.log'
    try {
        Add-Content -LiteralPath $script:CleanupLogPath -Value '' -Encoding UTF8
    }
    catch {
        $script:CleanupLogPath = $null
    }

    if (-not [string]::IsNullOrWhiteSpace($script:CleanupLogPath)) {
        Write-Host "Cleanup log: $script:CleanupLogPath"
    }
}

Write-CleanupLog -Level 'INFO' -Message "Cleanup script starting. Mode=$Mode"
Write-CleanupLog -Level 'INFO' -Message "Raw command line: $([Environment]::CommandLine)"
Write-CleanupLog -Level 'INFO' -Message "Env INSTR_PKGMGR_UNINSTALL_MODE=$env:INSTR_PKGMGR_UNINSTALL_MODE"

$requestedMode = $Mode
$envMode = [string]::Empty
if (-not [string]::IsNullOrWhiteSpace($env:INSTR_PKGMGR_UNINSTALL_MODE)) {
    $envMode = $env:INSTR_PKGMGR_UNINSTALL_MODE.Trim().ToUpperInvariant()
}

if ($envMode -eq 'SELF') {
    $Mode = 'Self'
    Write-CleanupLog -Level 'INFO' -Message "Cleanup mode overridden by environment variable to Self (requested=$requestedMode)."
}
elseif ($envMode -eq 'PURGE') {
    $Mode = 'Purge'
    Write-CleanupLog -Level 'INFO' -Message "Cleanup mode overridden by environment variable to Purge (requested=$requestedMode)."
}

function Get-ChocoPath {
    $choco = Join-Path $env:ProgramData 'chocolatey\bin\choco.exe'
    if (Test-Path -LiteralPath $choco) {
        return $choco
    }

    return 'choco'
}

function Read-PackageIdsFromWhitelist {
    param(
        [string] $WhitelistPath
    )

    if (-not (Test-Path -LiteralPath $WhitelistPath)) {
        return @()
    }

    try {
        $doc = [System.Xml.Linq.XDocument]::Load($WhitelistPath)
    }
    catch {
        Write-Warning "Failed to read whitelist file at $WhitelistPath"
        return @()
    }

    if ($null -eq $doc.Root) {
        return @()
    }

    $packageIds = @()
    foreach ($node in $doc.Descendants()) {
        $idAttribute = $node.Attribute('Id')
        if ($null -ne $idAttribute -and -not [string]::IsNullOrWhiteSpace($idAttribute.Value)) {
            $packageIds += $idAttribute.Value.Trim()
            continue
        }

        $packageIdAttribute = $node.Attribute('PackageId')
        if ($null -ne $packageIdAttribute -and -not [string]::IsNullOrWhiteSpace($packageIdAttribute.Value)) {
            $packageIds += $packageIdAttribute.Value.Trim()
            continue
        }

        if ($node.Name.LocalName -ieq 'Package' -and -not [string]::IsNullOrWhiteSpace($node.Value)) {
            $packageIds += $node.Value.Trim()
        }
    }

    return $packageIds | Select-Object -Unique
}

function Uninstall-ChocolateyPackage {
    param(
        [string] $ChocoPath,
        [string] $PackageId
    )

    if ([string]::IsNullOrWhiteSpace($PackageId)) {
        return
    }

    $arguments = @('uninstall', $PackageId, '-y', '-x', '-f')

    Write-Host "Uninstalling package $PackageId..."
    Write-CleanupLog -Level 'INFO' -Message "Uninstalling package $PackageId"
    try {
        & $ChocoPath @arguments
    }
    catch {
        Write-Warning "Failed to execute uninstall command for package ${PackageId}: $($_.Exception.Message)"
        Write-CleanupLog -Level 'WARN' -Message "Failed to execute uninstall command for package ${PackageId}: $($_.Exception.Message)"
        return $false
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to uninstall package $PackageId (exit code: $LASTEXITCODE)"
        Write-CleanupLog -Level 'WARN' -Message "Failed to uninstall package $PackageId (exit code: $LASTEXITCODE)"
        return $false
    }

    return $true
}

function Get-ChocolateyRoot {
    if (-not [string]::IsNullOrWhiteSpace($env:CHOCOLATEY_INSTALL) -and (Test-Path -LiteralPath $env:CHOCOLATEY_INSTALL)) {
        return $env:CHOCOLATEY_INSTALL
    }

    return (Join-Path $env:ProgramData 'chocolatey')
}

function Remove-ChocolateyPackageRegistration {
    param(
        [string] $ChocolateyRoot,
        [string] $PackageId
    )

    if ([string]::IsNullOrWhiteSpace($PackageId) -or [string]::IsNullOrWhiteSpace($ChocolateyRoot)) {
        return
    }

    $registrationPaths = @(
        (Join-Path $ChocolateyRoot ("lib\{0}" -f $PackageId)),
        (Join-Path $ChocolateyRoot ("lib-bkp\{0}" -f $PackageId)),
        (Join-Path $ChocolateyRoot ("lib-bad\{0}" -f $PackageId)),
        (Join-Path $ChocolateyRoot (".chocolatey\{0}.*" -f $PackageId))
    )

    foreach ($registrationPath in $registrationPaths) {
        $resolvedItems = @(Get-Item -Path $registrationPath -ErrorAction SilentlyContinue)
        foreach ($resolvedItem in $resolvedItems) {
            Write-Host "Removing stale Chocolatey registration path $($resolvedItem.FullName)..."
            Remove-Item -LiteralPath $resolvedItem.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Remove-ChocolateyShims {
    param(
        [string] $ChocolateyRoot,
        [string[]] $ShimBaseNames
    )

    if ([string]::IsNullOrWhiteSpace($ChocolateyRoot) -or $null -eq $ShimBaseNames -or $ShimBaseNames.Count -eq 0) {
        return
    }

    $binPath = Join-Path $ChocolateyRoot 'bin'
    if (-not (Test-Path -LiteralPath $binPath)) {
        return
    }

    foreach ($shimBaseName in $ShimBaseNames) {
        if ([string]::IsNullOrWhiteSpace($shimBaseName)) {
            continue
        }

        $shimCandidates = @(
            (Join-Path $binPath ("{0}.exe" -f $shimBaseName)),
            (Join-Path $binPath ("{0}.bat" -f $shimBaseName)),
            (Join-Path $binPath ("{0}.shim" -f $shimBaseName))
        )

        foreach ($shimPath in $shimCandidates) {
            if (Test-Path -LiteralPath $shimPath) {
                Write-Host "Removing Chocolatey shim $shimPath..."
                Remove-Item -LiteralPath $shimPath -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

$commonAppData = [Environment]::GetFolderPath('CommonApplicationData')
$configDirectory = Join-Path $commonAppData '__OEM_DIRECTORY_NAME__'
$whitelistPath = Join-Path $configDirectory 'chocogui-packages-whitelist.xml'
$offlinePackagesPath = Join-Path $configDirectory 'offline_packages'
$offlineSourceName = '__OFFLINE_SOURCE_NAME__'
$selfPackageId = '__PACKAGE_ID__'
$packageManagerCommandName = '__PACKAGE_COMMAND_NAME__'
$packageManagerCliCommandName = '__PACKAGE_CLI_COMMAND_NAME__'
$providedWhitelistPackageId = '__WHITELIST_PACKAGE_ID__'
$protectedPackageIds = @('chocolatey', $selfPackageId)

$choco = Get-ChocoPath
Write-CleanupLog -Level 'INFO' -Message "Resolved choco path: $choco"

$localAppDataRoot = [Environment]::GetFolderPath('LocalApplicationData')
$programFilesRoot = [Environment]::GetFolderPath('ProgramFiles')
$productDirectoryName = '__PRODUCT_DIRECTORY_NAME__'
$installRoot = Join-Path $programFilesRoot (Join-Path '__OEM_DIRECTORY_NAME__' $productDirectoryName)
$userAppDataPath = Join-Path $localAppDataRoot (Join-Path '__OEM_DIRECTORY_NAME__' $productDirectoryName)

if ($Mode -eq 'Self') {
    $chocoRoot = Get-ChocolateyRoot
    Write-CleanupLog -Level 'INFO' -Message "Running self cleanup. Chocolatey root: $chocoRoot"
    # Do not call "choco uninstall" here: MSI uninstall can be triggered from choco,
    # and re-entering package uninstall introduces recursion/order risks.
    Remove-ChocolateyPackageRegistration -ChocolateyRoot $chocoRoot -PackageId $selfPackageId
    Remove-ChocolateyShims -ChocolateyRoot $chocoRoot -ShimBaseNames @($packageManagerCommandName, $packageManagerCliCommandName, $selfPackageId, "$selfPackageId-cli")

    Write-CleanupLog -Level 'INFO' -Message 'Self cleanup completed.'

    exit 0
}

$packageIds = @(Read-PackageIdsFromWhitelist -WhitelistPath $whitelistPath)
$packageIds += @(
    $providedWhitelistPackageId
)

$packageIds = $packageIds |
Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
Where-Object { $_ -notin $protectedPackageIds } |
Select-Object -Unique

Write-CleanupLog -Level 'INFO' -Message ("Packages selected for purge cleanup: {0}" -f ($packageIds -join ', '))

foreach ($packageId in $packageIds) {
    [void](Uninstall-ChocolateyPackage -ChocoPath $choco -PackageId $packageId)
}

$chocoRoot = Get-ChocolateyRoot
Write-CleanupLog -Level 'INFO' -Message "Removing self package registration from Chocolatey root: $chocoRoot"
Remove-ChocolateyPackageRegistration -ChocolateyRoot $chocoRoot -PackageId $selfPackageId
Remove-ChocolateyShims -ChocolateyRoot $chocoRoot -ShimBaseNames @($packageManagerCommandName, $packageManagerCliCommandName, $selfPackageId, "$selfPackageId-cli")

Write-Host "Removing Chocolatey source $offlineSourceName..."
Write-CleanupLog -Level 'INFO' -Message "Removing Chocolatey source $offlineSourceName"
try {
    & $choco source remove --name $offlineSourceName
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to remove Chocolatey source $offlineSourceName (exit code: $LASTEXITCODE)"
        Write-CleanupLog -Level 'WARN' -Message "Failed to remove Chocolatey source $offlineSourceName (exit code: $LASTEXITCODE)"
    }
}
catch {
    Write-Warning "Failed to remove Chocolatey source ${offlineSourceName}: $($_.Exception.Message)"
    Write-CleanupLog -Level 'WARN' -Message "Failed to remove Chocolatey source ${offlineSourceName}: $($_.Exception.Message)"
}

$cleanupPaths = @(
    $offlinePackagesPath,
    $whitelistPath,
    $configDirectory,
    $installRoot,
    $userAppDataPath
)

foreach ($cleanupPath in $cleanupPaths) {
    if (-not (Test-Path -LiteralPath $cleanupPath)) {
        continue
    }

    Write-Host "Removing path $cleanupPath..."
    Write-CleanupLog -Level 'INFO' -Message "Removing path $cleanupPath"
    try {
        Remove-Item -LiteralPath $cleanupPath -Recurse -Force -ErrorAction Stop
    }
    catch {
        Write-Warning "Failed to remove path ${cleanupPath}: $($_.Exception.Message)"
        Write-CleanupLog -Level 'WARN' -Message "Failed to remove path ${cleanupPath}: $($_.Exception.Message)"
    }

    if (Test-Path -LiteralPath $cleanupPath) {
        Write-Warning "Path still exists after cleanup attempt: $cleanupPath"
        Write-CleanupLog -Level 'WARN' -Message "Path still exists after cleanup attempt: $cleanupPath"
    }
}

Write-CleanupLog -Level 'INFO' -Message 'Purge cleanup completed.'
