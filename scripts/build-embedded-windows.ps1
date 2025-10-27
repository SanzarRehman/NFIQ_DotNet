param(
    [switch]$X64 = $true,
    [switch]$X86 = $true
)

$ErrorActionPreference = 'Stop'

# Ensure CMake in PATH
$cmakeDir = 'C:\Program Files\CMake\bin'
if (Test-Path $cmakeDir) { $env:Path += ";$cmakeDir" }

function Build-One {
    param(
        [string]$Arch, # 'x64' or 'Win32'
        [string]$BuildDir,
        [string]$DistDir
    )

    Write-Host "Configuring $Arch (embedded model)..."
    cmake -S . -B $BuildDir -G "Visual Studio 17 2022" -A $Arch -DBUILD_NFIQ2_CLI=OFF -DEMBED_RANDOM_FOREST_PARAMETERS=ON -DCMAKE_CONFIGURATION_TYPES=Release

    Write-Host "Building $Arch Release nfiq2api..."
    cmake --build $BuildDir --config Release --target nfiq2api -- /m

    New-Item -ItemType Directory -Force $DistDir | Out-Null

    $dll = Join-Path $BuildDir 'nfiq2api-prefix/src/nfiq2api-build/Release/Nfiq2Api.dll'
    Copy-Item -Force $dll $DistDir

    $frfx = Join-Path $BuildDir 'install_staging/nfiq2/bin/FRFXLL.dll'
    if (Test-Path $frfx) { Copy-Item -Force $frfx $DistDir }

    # No YAML needed when embedded
}

if ($X64) {
    Build-One -Arch 'x64' -BuildDir 'build' -DistDir 'dist/win-x64'
}
if ($X86) {
    Build-One -Arch 'Win32' -BuildDir 'build32' -DistDir 'dist/win-x86'
}

Write-Host "Done. Output in dist/win-x64 and/or dist/win-x86."
