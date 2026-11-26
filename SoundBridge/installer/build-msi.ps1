Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$InstallerDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = Split-Path -Parent $InstallerDir
$MetaPath = Join-Path $Root "app.meta.json"
$PublishDir = Join-Path $Root "dist\SoundBridge"
$InstallerOutputDir = Join-Path $Root "dist\installer"
$PackagePath = Join-Path $InstallerDir "Package.wxs"
$AppIconPath = Join-Path $Root "app\assets\icon.ico"

if (-not (Test-Path -LiteralPath $MetaPath)) {
    throw "Missing app metadata file: $MetaPath"
}

$meta = Get-Content -LiteralPath $MetaPath -Raw | ConvertFrom-Json

$productName = [string]$meta.product.name
$manufacturer = [string]$meta.product.manufacturer
$productVersion = [string]$meta.product.version
$displayVersion = [string]$meta.product.displayVersion
$executableName = [string]$meta.product.executableName
$upgradeCode = [string]$meta.installer.upgradeCode
$productCodeMode = [string]$meta.installer.productCodeMode
$packageCodeStrategy = [string]$meta.installer.packageCodeStrategy

if ([string]::IsNullOrWhiteSpace($productName)) { throw "app.meta.json product.name is required." }
if ([string]::IsNullOrWhiteSpace($manufacturer)) { throw "app.meta.json product.manufacturer is required." }
if ([string]::IsNullOrWhiteSpace($productVersion)) { throw "app.meta.json product.version is required." }
if ([string]::IsNullOrWhiteSpace($executableName)) { throw "app.meta.json product.executableName is required." }
if ([string]::IsNullOrWhiteSpace($upgradeCode)) { throw "app.meta.json installer.upgradeCode is required." }

if ([string]::IsNullOrWhiteSpace($displayVersion)) {
    $displayVersion = "v$productVersion"
}

$expectedDisplayVersion = "v $productVersion"
if ($displayVersion -ne $expectedDisplayVersion) {
    throw "app.meta.json product.displayVersion must be '$expectedDisplayVersion' when product.version is '$productVersion'."
}

if ($productCodeMode -ne "perVersion") {
    throw "app.meta.json installer.productCodeMode must be 'perVersion'."
}

if ($packageCodeStrategy -ne "auto") {
    throw "app.meta.json installer.packageCodeStrategy must be 'auto' for MSI package builds."
}

try {
    [void][Guid]::Parse($upgradeCode)
}
catch {
    throw "app.meta.json installer.upgradeCode must be a valid GUID."
}

$productCodes = $meta.installer.productCodes
if ($null -eq $productCodes) {
    $productCodes = [pscustomobject]@{}
    $meta.installer | Add-Member -MemberType NoteProperty -Name "productCodes" -Value $productCodes
}

$productCodeProperty = $productCodes.PSObject.Properties[$productVersion]
if ($null -eq $productCodeProperty -or [string]::IsNullOrWhiteSpace([string]$productCodeProperty.Value)) {
    $generatedProductCode = [Guid]::NewGuid().ToString().ToUpperInvariant()
    if ($null -eq $productCodeProperty) {
        $productCodes | Add-Member -MemberType NoteProperty -Name $productVersion -Value $generatedProductCode
    }
    else {
        $productCodeProperty.Value = $generatedProductCode
    }

    $meta | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $MetaPath -Encoding UTF8
    $productCode = $generatedProductCode
    Write-Host "Generated ProductCode for version ${productVersion}: $productCode"
    Write-Host "Updated app.meta.json"
}
else {
    $productCode = [string]$productCodeProperty.Value
}

try {
    [void][Guid]::Parse($productCode)
}
catch {
    throw "app.meta.json installer.productCodes['$productVersion'] must be a valid GUID."
}

$safeDisplayVersion = ($displayVersion -replace '\s+', '')
$invalidFileNameChars = [System.IO.Path]::GetInvalidFileNameChars()
$safeProductName = $productName
foreach ($char in $invalidFileNameChars) {
    $safeProductName = $safeProductName.Replace([string]$char, '')
}

if ([string]::IsNullOrWhiteSpace($safeProductName)) {
    throw "app.meta.json product.name must contain at least one valid filename character."
}

$versionedOutputName = "$safeProductName`Setup-$safeDisplayVersion.msi"

if (-not (Test-Path -LiteralPath $AppIconPath)) {
    throw "Missing app icon: $AppIconPath"
}

$wix = Get-Command wix -ErrorAction SilentlyContinue
if ($null -eq $wix) {
    throw @"
WiX CLI was not found.

Install WiX Toolset before building the MSI:

  dotnet tool install --global wix
  wix extension add WixToolset.UI.wixext

Then rerun:

  .\installer\build-msi.bat
"@
}

function Assert-LastCommandSucceeded {
    param([string]$StepName)

    if ($LASTEXITCODE -ne 0) {
        throw "$StepName failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Building backend..."
$BackendBuildDir = Join-Path $Root "backend\build-integration"
$CmakeCache = Join-Path $BackendBuildDir "CMakeCache.txt"
if (Test-Path -LiteralPath $CmakeCache) {
    Write-Host "Clearing stale CMake cache..."
    Remove-Item -LiteralPath $CmakeCache -Force
}
& cmake -S (Join-Path $Root "backend") -B $BackendBuildDir -A x64
Assert-LastCommandSucceeded "Backend configure"
& cmake --build $BackendBuildDir --config Release
Assert-LastCommandSucceeded "Backend build"

Write-Host "Publishing GUI..."
& dotnet publish (Join-Path $Root "app\SoundBridge.Gui.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $PublishDir
Assert-LastCommandSucceeded "GUI publish"

$publishedExe = Join-Path $PublishDir $executableName
if (-not (Test-Path -LiteralPath $publishedExe)) {
    throw "Published executable not found: $publishedExe"
}

$bridgeDll = Join-Path $PublishDir "SoundBridgeBridge.dll"
if (-not (Test-Path -LiteralPath $bridgeDll)) {
    throw "Native bridge DLL not found in publish output: $bridgeDll"
}

New-Item -ItemType Directory -Force -Path $InstallerOutputDir | Out-Null

$outputPath = Join-Path $InstallerOutputDir $versionedOutputName

Write-Host "Building MSI..."
& wix build $PackagePath `
    -acceptEula wix7 `
    -arch x64 `
    -ext WixToolset.UI.wixext `
    -d "ProjectDir=$InstallerDir" `
    -d "PublishDir=$PublishDir" `
    -d "ProductName=$productName" `
    -d "Manufacturer=$manufacturer" `
    -d "ProductVersion=$productVersion" `
    -d "ExecutableName=$executableName" `
    -d "ProductCode=$productCode" `
    -d "UpgradeCode=$upgradeCode" `
    -d "AppIconPath=$AppIconPath" `
    -out $outputPath
Assert-LastCommandSucceeded "MSI build"

Write-Host "MSI created: $outputPath"
