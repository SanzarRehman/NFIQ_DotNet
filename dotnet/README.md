# NFIQ2 .NET Interop

This folder contains a managed .NET wrapper around the native NFIQ2 SDK and a small console sample that demonstrates how to compute NFIQ2 scores from grayscale fingerprint images. The wrapper is designed to make it easy to use NFIQ2 in C# applications (including those that integrate with SecuGen readers).

## Repository layout

| Path | Purpose |
| --- | --- |
| `Nfiq2DotNet/` | .NET class library that P/Invokes the native `Nfiq2Api` shared library. |
| `Nfiq2DotNet.Sample/` | Console application that exercises the managed wrapper using `.pgm` fingerprint images. |

Both projects target cross-platform runtimes:

- `Nfiq2DotNet` targets **.NET Standard 2.0** so it can be loaded by .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 6/7/8 apps.
- `Nfiq2DotNet.Sample` targets **.NET 6.0** and references the class library.

## Building the native NFIQ2 DLL

The managed wrapper assumes that the native `Nfiq2Api` shared library has already been built. You can build it with the superbuild that ships in this repository.

### Windows (x64)

```powershell
# From the repository root
cmake -S . -B build -G "Visual Studio 17 2022" -A x64 -DBUILD_NFIQ2_CLI=OFF
cmake --build build --config Release --target nfiq2api
```

After a successful build the DLL will be available at:

```
build\nfiq2api-prefix\src\nfiq2api-build\Release\Nfiq2Api.dll
```

Copy `Nfiq2Api.dll` (and its dependencies such as `libFRFXLL.dll`) next to your .NET application's executable or place the folder on the `PATH` so the runtime can load it.

#### Building a 32-bit (x86) DLL

If your .NET application runs as a 32-bit process, generate the superbuild with Win32 tools and optionally set `32BITS=ON`:

```powershell
cmake -S . -B build32 -G "Visual Studio 17 2022" -A Win32 -DBUILD_NFIQ2_CLI=OFF -D32BITS=ON
cmake --build build32 --config Release --target nfiq2api
```

The resulting binary will be located at `build32\nfiq2api-prefix\src\nfiq2api-build\Release\Nfiq2Api.dll`. Be sure every native dependency (e.g., `libFRFXLL.dll`) also comes from the 32-bit build tree.

### Linux & macOS

```bash
# From the repository root
cmake -S . -B build -DBUILD_NFIQ2_CLI=OFF
cmake --build build --target nfiq2api
```

Relevant outputs:

- Linux: `build/nfiq2api-prefix/src/nfiq2api-build/libNfiq2Api.so`
- macOS: `build/nfiq2api-prefix/src/nfiq2api-build/libNfiq2Api.dylib`

Copy the shared library into a location discoverable by the dynamic loader (e.g., alongside the managed binaries or a directory listed in `LD_LIBRARY_PATH`/`DYLD_LIBRARY_PATH`).

> **Tip:** If you only need the DLL and headers you can disable the CLI to shorten build time, as shown above.

#### Building 32-bit binaries on Linux

To target 32-bit runtimes, ensure the toolchain has multilib support installed (e.g., `gcc-multilib`/`g++-multilib` on Debian/Ubuntu) and configure the build with explicit 32-bit flags:

```bash
cmake -S . -B build32 -DBUILD_NFIQ2_CLI=OFF -D32BITS=ON -DCMAKE_C_COMPILER=gcc -DCMAKE_CXX_COMPILER=g++ -DCMAKE_C_FLAGS="-m32" -DCMAKE_CXX_FLAGS="-m32"
cmake --build build32 --target nfiq2api
```

This produces a 32-bit `libNfiq2Api.so` under `build32/nfiq2api-prefix/src/nfiq2api-build/`. Deploy the matching 32-bit versions of every dependent shared object alongside your managed application.

## Building the .NET projects

```bash
cd dotnet
# Build the class library
 dotnet build Nfiq2DotNet/Nfiq2DotNet.csproj
# Build the sample app
 dotnet build Nfiq2DotNet.Sample/Nfiq2DotNet.Sample.csproj
```

Artifacts are placed under each project's `bin/<Configuration>/<TargetFramework>` folder.

## Using the managed API

### Quick start from C#

```csharp
using Nfiq2DotNet;

// Optionally specify an explicit path to Nfiq2Api.dll / .so / .dylib
using var nfiq = new Nfiq2Algorithm();
string modelHash = nfiq.Initialise();

// Acquire or decode a fingerprint image (8-bit grayscale)
byte[] pixels = LoadSecuGenImage();
int width = 300; // pixels
int height = 400; // pixels

int score = nfiq.ComputeScore(
    pixels,
    width,
    height,
    FingerPosition.RightIndex,
    ppi: 500);

Console.WriteLine($"NFIQ2 score = {score}");
```

Key points:

- Call `Initialise()` once per process to load the model and random forest parameters. The call returns the parameter hash reported by the native model.
- Provide raw 8-bit grayscale fingerprint data. SecuGen SDKs typically expose a raw buffer; ensure it is not compressed and matches `width * height` bytes.
- Specify the finger position if known. The default `FingerPosition.Unknown` matches the native API's default.
- Dispose the `Nfiq2Algorithm` instance when done to release native resources.

### SecuGen integration notes

1. Use the SecuGen SDK to capture a fingerprint in 8-bit grayscale. Convert the buffer into a `byte[]` without additional headers.
2. Supply the SecuGen image metadata (width, height, PPI). Many SecuGen devices capture at 500 PPI.
3. If you receive images in proprietary formats, convert them to raw 8-bit grayscale before invoking `ComputeScore`.
4. Handle exceptions of type `Nfiq2Exception`, which provides descriptive messages propagated from the native library.

## Sample console

The `Nfiq2DotNet.Sample` project accepts a `.pgm` (P5) grayscale fingerprint image and prints the computed score.

```bash
dotnet run --project dotnet/Nfiq2DotNet.Sample \
  -- <path>/SFinGe_Test01.pgm --finger=RightIndex --ppi=500 \
  --lib=/absolute/path/to/Nfiq2Api.dll
```

- `--finger` is optional; omit it if the finger position is unknown.
- `--ppi` defaults to 500.
- `--lib` can point to the native library if it is not on the default search path.

Example output:

```
Computed NFIQ2 score: 41
Parameter hash:      ccd75820b48c19f1645ef5e9c481c592
Image size:          512x512 @ 500 PPI
Finger position:     RightIndex
Library version:     2.3.0 (OpenCV 4.5.4)
```

## Troubleshooting

| Symptom | Possible cause & resolution |
| --- | --- |
| `DllNotFoundException: Unable to load NFIQ2 native library` | Ensure `Nfiq2Api` is on the OS search path or pass an explicit path to `Initialise(path)`. |
| `Nfiq2Exception` with hash load failure | Verify that the YAML model file is co-located with the native library (or rebuild with embedded parameters). |
| BadImageFormatException (Windows) | Bitness mismatch; ensure both the native DLL and .NET process are either x86 or x64. |

## Next steps

- Add unit tests exercising typical SecuGen capture sizes.
- Package `Nfiq2DotNet` as a NuGet package with native assets for distribution.
- Wire the managed wrapper into your SecuGen capture pipeline, ensuring threading requirements of the native SDK are respected.
