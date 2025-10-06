# NFIQ2 .NET Integration Guide

This guide explains how to consume the native `Nfiq2Api` library from your own .NET application using the `Nfiq2DotNet` wrapper contained in this repository.

## 1. Build native binaries

Windows x64 (Release):
```powershell
cmake -S . -B build -G "Visual Studio 17 2022" -A x64 -DBUILD_NFIQ2_CLI=OFF -DCMAKE_CONFIGURATION_TYPES=Release
cmake --build build --config Release --target nfiq2api -- /m
```
Outputs (copy one of these):
- `build\nfiq2api-prefix\src\nfiq2api-build\Release\Nfiq2Api.dll` (primary)
- Dependent: `build\install_staging\nfiq2\bin\FRFXLL.dll` (if produced)

Windows x86 (if needed):
```powershell
cmake -S . -B build32 -G "Visual Studio 17 2022" -A Win32 -DBUILD_NFIQ2_CLI=OFF -D32BITS=ON -DCMAKE_CONFIGURATION_TYPES=Release
cmake --build build32 --config Release --target nfiq2api -- /m
```

Linux/macOS:
```bash
cmake -S . -B build -DBUILD_NFIQ2_CLI=OFF
cmake --build build --target nfiq2api -j
```
Artifacts:
- Linux: `build/nfiq2api-prefix/src/nfiq2api-build/libNfiq2Api.so`
- macOS: `build/nfiq2api-prefix/src/nfiq2api-build/libNfiq2Api.dylib`

## 2. Place model file (if not embedded)
If built without `NFIQ2_EMBED_RANDOM_FOREST_PARAMETERS`, copy `NFIQ2/nist_plain_tir-ink.yaml` next to the native library *or* supply an absolute path when constructing the algorithm (wrapper auto-locates if co‑located).

## 3. Add the managed wrapper
Reference the project or (future) NuGet:
```bash
dotnet add <YourProject>.csproj reference ../dotnet/Nfiq2DotNet/Nfiq2DotNet.csproj
```
OR copy the `Nfiq2DotNet` folder into your solution and reference it.

Target compatibility:
- Library: .NET Standard 2.0 (works with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 6/7/8)

## 4. Deploy native library
Place the native library (and any dependent DLLs/SOs) in one of:
1. Same directory as your app executable.
2. A directory on `PATH` (Windows) / `LD_LIBRARY_PATH` (Linux) / `DYLD_LIBRARY_PATH` (macOS).
3. Provide an explicit path to `Initialise(pathToLib)` (if wrapper extended to accept path).

Bitness must match the process (x64 ↔ x64, x86 ↔ x86). A `BadImageFormatException` indicates mismatch.

## 5. Basic usage
```csharp
using var algo = new Nfiq2DotNet.Nfiq2Algorithm();
string modelHash = algo.Initialise();
int score = algo.ComputeScore(pixels, width, height, Nfiq2DotNet.FingerPosition.RightIndex, 500);
```
Requirements:
- `pixels.Length == width * height`
- 8‑bit grayscale raw fingerprint image

Returned score range: 0–100 (higher = better). Error conditions throw `Nfiq2Exception` or return negative codes in native layer before being surfaced.

## 6. Error handling
Common issues:
- `DllNotFoundException`: Native library not found. Ensure location or path.
- `Nfiq2Exception` with hash mismatch: YAML file missing/incorrect or wrong working directory.
- `BadImageFormatException`: Architecture mismatch.

Retrieve last native error (debug): internal P/Invoke uses `GetLastNfiq2Error()`; managed wrapper converts to exceptions automatically.

## 7. Threading
You may call `ComputeScore` concurrently from multiple threads after a single `Initialise()`. The wrapper holds a single native algorithm instance—serialize initialization; scoring is internally read‑only.

## 8. Performance notes
- Avoid re-initializing per image; `Initialise()` loads the model and random forest once.
- Pinned arrays are not required; `byte[]` is copied by the marshaller.
- For high throughput, reuse one `Nfiq2Algorithm` per thread if contention becomes noticeable.

## 9. Packaging (optional NuGet)
Structure suggestion:
```
/runtimes/win-x64/native/Nfiq2Api.dll
/runtimes/win-x86/native/Nfiq2Api.dll
/runtimes/linux-x64/native/libNfiq2Api.so
/runtimes/osx-x64/native/libNfiq2Api.dylib
/lib/netstandard2.0/Nfiq2DotNet.dll
```
Include YAML in each `native` folder if not embedded.

## 10. Embedding model parameters
Add `-DNFIQ2_EMBED_RANDOM_FOREST_PARAMETERS=ON` at configure time to bake parameters into the binary and avoid shipping the YAML file. Hash returned by `Initialise()` should match the known model hash.

## 11. SecuGen integration tips
- Use SDK capture callback to get raw grayscale buffer (usually 500 PPI).
- Validate buffer size immediately: `if (buf.Length != width*height) throw`. 
- Pass enumerated finger position when known; else use `FingerPosition.Unknown`.

## 12. Troubleshooting quick table
| Symptom | Fix |
| ------- | --- |
| Score always -2 | Call `Initialise()` before `ComputeScore` or bitness mismatch prevented load. |
| Score always same | Confirm you are feeding distinct images; check capture pipeline. |
| Crash on load | Missing dependent DLL (e.g., OpenCV or FRFXLL); ensure they sit with `Nfiq2Api.dll`. |
| High memory use | Building with full OpenCV; consider trimming build or Release-only configuration. |

## 13. Cleaning / rebuilding
```powershell
Remove-Item -Recurse -Force build
cmake -S . -B build -G "Visual Studio 17 2022" -A x64 -DBUILD_NFIQ2_CLI=OFF -DCMAKE_CONFIGURATION_TYPES=Release
cmake --build build --config Release --target nfiq2api -- /m
```

## 14. License
Refer to `LICENSE.md` (NIST / public domain notices) and submodule licenses (OpenCV, digestpp, FingerJetFXOSE, libbiomeval, NFIR) for redistribution requirements.

---
Minimal integration steps recap:
1. Build native `Nfiq2Api` for required architectures.
2. Copy native binaries (and YAML if needed) next to your app.
3. Reference or build `Nfiq2DotNet`.
4. Call `Initialise()` once, then `ComputeScore()` per image.
