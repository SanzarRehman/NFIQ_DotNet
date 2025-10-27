# NFIQ2 Windows Build and Package (Embedded Model)

This document shows how to build NFIQ2 native libraries with embedded random forest parameters (no YAML at runtime) on Windows using Visual Studio 2022 and package the output for .NET consumption.

## Prerequisites
- Visual Studio 2022 with C++ desktop workload
- CMake in PATH (e.g., `C:\Program Files\CMake\bin`)
- PowerShell 7+ recommended
- 6–8 GB free disk space

## Quick script (build x64 and x86 and package)
Use the helper script under `scripts/`:

```powershell
pwsh -File scripts/build-embedded-windows.ps1
```

This will:
- Configure and build x64 and x86 Release with `-DEMBED_RANDOM_FOREST_PARAMETERS=ON`
- Package DLLs to `dist/win-x64` and `dist/win-x86`
- Exclude the YAML from packages (not needed when embedded)

## Manual build: x64 only (Release)
```powershell
$env:Path += ';C:\Program Files\CMake\bin'
cmake -S . -B build -G "Visual Studio 17 2022" -A x64 -DBUILD_NFIQ2_CLI=OFF -DEMBED_RANDOM_FOREST_PARAMETERS=ON -DCMAKE_CONFIGURATION_TYPES=Release
cmake --build build --config Release --target nfiq2api -- /m

# Package
New-Item -ItemType Directory -Force dist/win-x64 | Out-Null
Copy-Item build/nfiq2api-prefix/src/nfiq2api-build/Release/Nfiq2Api.dll dist/win-x64 -Force
# Optional FRFXLL if produced
if (Test-Path build/install_staging/nfiq2/bin/FRFXLL.dll) { Copy-Item build/install_staging/nfiq2/bin/FRFXLL.dll dist/win-x64 -Force }
```

## Manual build: x86 only (Release)
```powershell
$env:Path += ';C:\Program Files\CMake\bin'
cmake -S . -B build32 -G "Visual Studio 17 2022" -A Win32 -DBUILD_NFIQ2_CLI=OFF -DEMBED_RANDOM_FOREST_PARAMETERS=ON -D32BITS=ON -DCMAKE_CONFIGURATION_TYPES=Release
cmake --build build32 --config Release --target nfiq2api -- /m

# Package
New-Item -ItemType Directory -Force dist/win-x86 | Out-Null
Copy-Item build32/nfiq2api-prefix/src/nfiq2api-build/Release/Nfiq2Api.dll dist/win-x86 -Force
if (Test-Path build32/install_staging/nfiq2/bin/FRFXLL.dll) { Copy-Item build32/install_staging/nfiq2/bin/FRFXLL.dll dist/win-x86 -Force }
```

## Output locations
- x64 DLL: `build/nfiq2api-prefix/src/nfiq2api-build/Release/Nfiq2Api.dll`
- x86 DLL: `build32/nfiq2api-prefix/src/nfiq2api-build/Release/Nfiq2Api.dll`
- Packages: `dist/win-x64/`, `dist/win-x86/`

## Using with .NET
Copy the packaged folder matching your process bitness next to your app executable. With embedding enabled, `nist_plain_tir-ink.yaml` is NOT required at runtime.

## Tips
- If disk space is low, delete `build/OpenCV-prefix` after one architecture finishes to free space.
- Use short paths (e.g., `C:\NFIQ2`) to avoid path length issues.
- For verbose MSBuild output, add `-v:n` after the build command.
