# NFIQ2 .NET Quick Start

Minimal steps to use the already built native `Nfiq2Api` in your .NET application.

## 1. Prerequisites
- You have `Nfiq2Api.dll` (and `FRFXLL.dll` if produced) built for the same architecture (x64 or x86) as your .NET process.
- (If not built with embedded parameters) You have the model file `nist_plain_tir-ink.yaml`.

## 2. Place Native Assets
Copy these files into your app output folder (e.g. `bin/Release/net6.0/`):
- `Nfiq2Api.dll`
- `FRFXLL.dll` (and any other dependent DLLs, if present)
- `nist_plain_tir-ink.yaml` (if parameters not embedded)

Alternatively add their directory to the `PATH` before first use.

## 3. Add Managed Wrapper
Reference the wrapper project:
```powershell
dotnet add YourApp.csproj reference ..\dotnet\Nfiq2DotNet\Nfiq2DotNet.csproj
```
Or reference the compiled `Nfiq2DotNet.dll` directly.

## 4. Basic Code
```csharp
using Nfiq2DotNet;

// Raw 8-bit grayscale fingerprint image (width * height bytes)
byte[] pixels = LoadFingerprint();
int width = 300;
int height = 400;
int ppi = 500; // common

using var nfiq = new Nfiq2Algorithm();
string paramHash = nfiq.Initialise();  // loads model once
int score = nfiq.ComputeScore(pixels, width, height, FingerPosition.RightIndex, ppi);

Console.WriteLine($"Score={score} ParamHash={paramHash}");
```
Requirements:
- `pixels.Length == width * height`
- 8-bit grayscale (no headers/compression)
- Call `Initialise()` first (once)

## 5. Finger Position Enum
Use `FingerPosition.Unknown` if you do not know the finger. Otherwise pass the correct value to improve feature handling.

## 6. Threading
Call `Initialise()` once; then you can call `ComputeScore` from multiple threads safely. Reuse the same `Nfiq2Algorithm` instance or create one per thread if preferred.

## 7. Common Errors
| Issue | Cause / Fix |
| ----- | ----------- |
| `DllNotFoundException` | `Nfiq2Api.dll` not found; place next to exe or add folder to PATH. |
| `BadImageFormatException` | Architecture mismatch (x86 vs x64). Rebuild or switch process bitness. |
| YAML hash / init failure | Missing `nist_plain_tir-ink.yaml`; rebuild with `-DNFIQ2_EMBED_RANDOM_FOREST_PARAMETERS=ON` to avoid external file. |
| Negative return / exception | Validate image size and metadata (width, height, ppi, pixel buffer length). |

## 8. Cleanup
Disposing (`using`) releases native resources; app exit also unloads the library.

## 9. Optional: Embedded Model
Rebuild native library with:
```powershell
cmake -S . -B build -A x64 -DNFIQ2_EMBED_RANDOM_FOREST_PARAMETERS=ON -DBUILD_NFIQ2_CLI=OFF -DCMAKE_CONFIGURATION_TYPES=Release
cmake --build build --config Release --target nfiq2api -- /m
```
Then you do not need the YAML file at runtime.

## 10. Summary
1. Copy native DLLs (+ YAML if needed) next to your app.
2. Reference `Nfiq2DotNet`.
3. Initialise once.
4. Compute scores.




# Ensure CMake available
$cmakeDir = 'C:\Program Files\CMake\bin'
if (Test-Path $cmakeDir) { $env:Path += ";$cmakeDir" }

Write-Host 'Re-configuring with EMBED_RANDOM_FOREST_PARAMETERS=ON'
cmake -S . -B build -G "Visual Studio 17 2022" -A x64 -DBUILD_NFIQ2_CLI=OFF -DEMBED_RANDOM_FOREST_PARAMETERS=ON -DCMAKE_CONFIGURATION_TYPES=Release
if ($LASTEXITCODE -ne 0) { throw 'CMake configure failed' }

Write-Host 'Building embedded nfiq2api (Release)...'
cmake --build build --config Release --target nfiq2api -- /m
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

# Prepare distribution folder
$dist = Join-Path $PWD 'dist/win-x64'
if (!(Test-Path $dist)) { New-Item -ItemType Directory -Force -Path $dist | Out-Null }

# Copy new DLL
Copy-Item -Force 'build/nfiq2api-prefix/src/nfiq2api-build/Release/Nfiq2Api.dll' $dist

# Remove YAML from dist (not needed now)
$yaml = Join-Path $dist 'nist_plain_tir-ink.yaml'
if (Test-Path $yaml) { Remove-Item -Force $yaml }
