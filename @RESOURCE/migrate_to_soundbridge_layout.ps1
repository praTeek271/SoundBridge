Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$TargetRoot = Join-Path $Root "SoundBridge"

function Assert-InWorkspace {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $rootFull = [System.IO.Path]::GetFullPath($Root)
    if (-not $resolved.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside workspace: $resolved"
    }
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    Assert-InWorkspace $Path
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Move-One {
    param(
        [Parameter(Mandatory = $true)]
        [string] $From,

        [Parameter(Mandatory = $true)]
        [string] $To
    )

    $source = Join-Path $Root $From
    $destination = Join-Path $Root $To

    Assert-InWorkspace $source
    Assert-InWorkspace $destination

    if (-not (Test-Path -LiteralPath $source)) {
        Write-Host "skip missing: $From"
        return
    }

    if (Test-Path -LiteralPath $destination) {
        Write-Host "skip existing: $To"
        return
    }

    Ensure-Directory (Split-Path -Parent $destination)
    Write-Host "move: $From -> $To"
    Move-Item -LiteralPath $source -Destination $destination
}

function Move-TreeFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FromRoot,

        [Parameter(Mandatory = $true)]
        [string] $ToRoot,

        [string[]] $ExcludeDirectoryNames = @()
    )

    $sourceRoot = Join-Path $Root $FromRoot
    if (-not (Test-Path -LiteralPath $sourceRoot)) {
        Write-Host "skip missing tree: $FromRoot"
        return
    }

    Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Force |
        Where-Object {
            $relative = [System.IO.Path]::GetRelativePath($sourceRoot, $_.FullName)
            $parts = $relative -split '[\\/]'
            -not ($parts | Where-Object { $ExcludeDirectoryNames -contains $_ })
        } |
        ForEach-Object {
            $relative = [System.IO.Path]::GetRelativePath($sourceRoot, $_.FullName)
            Move-One (Join-Path $FromRoot $relative) (Join-Path $ToRoot $relative)
        }
}

Ensure-Directory $TargetRoot
Ensure-Directory (Join-Path $TargetRoot "app")
Ensure-Directory (Join-Path $TargetRoot "backend")
Ensure-Directory (Join-Path $TargetRoot "docs")
Ensure-Directory (Join-Path $TargetRoot "docs\prototypes")
Ensure-Directory (Join-Path $TargetRoot "drivers.test")
Ensure-Directory (Join-Path $TargetRoot "installer")
Ensure-Directory (Join-Path $TargetRoot "dist")

# WPF GUI source. Generated Debug/Release folders are intentionally skipped.
Move-TreeFiles "app" "SoundBridge\app" @("bin", "obj")

# C++ audio backend source. CMake/MSBuild output folders are intentionally skipped.
Move-One "native\SoundBridge.Native\CMakeLists.txt" "SoundBridge\backend\CMakeLists.txt"
Move-One "native\SoundBridge.Native\README.md" "SoundBridge\backend\README.md"
Move-TreeFiles "native\SoundBridge.Native\src" "SoundBridge\backend\src"
Move-TreeFiles "native\SoundBridge.Native\scripts" "SoundBridge\backend\scripts"

# Project documentation and prototype/reference files.
Move-TreeFiles "docs" "SoundBridge\docs"
Move-One "application_visual_layer_prototype_v3_2.html" "SoundBridge\docs\prototypes\application_visual_layer_prototype_v3_2.html"
Move-One "file_structure.txt" "SoundBridge\docs\file_structure.txt"

# Driver test package. This remains separate from the app/backend lifecycle.
Move-TreeFiles "Virtual.Audio.Driver.Signed.-.25.7.14" "SoundBridge\drivers.test\Virtual.Audio.Driver.Signed.-.25.7.14"

# Repo-level metadata for the new wrapper root.
Move-One ".gitignore" "SoundBridge\.gitignore"

Write-Host ""
Write-Host "Migration script completed."
Write-Host "Generated folders intentionally left behind: app\bin, app\obj, dist, native build folders, .vs."
Write-Host "Next step after running: update paths in SoundBridge\app\SoundBridge.Gui.csproj and SoundBridge\docs\installation.md."
