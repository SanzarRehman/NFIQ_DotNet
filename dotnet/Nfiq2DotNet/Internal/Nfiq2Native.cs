using System;
using System.Runtime.InteropServices;

namespace Nfiq2DotNet.Internal;

internal static class Nfiq2Native
{
    private const string LibraryBaseName = "Nfiq2Api";

    private static readonly object SyncRoot = new object();
    private static IntPtr _libraryHandle = IntPtr.Zero;

    internal static string NativeLibraryName
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Nfiq2Api.dll";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "libNfiq2Api.dylib";
            }

            return "libNfiq2Api.so";
        }
    }

    internal static void EnsureLibraryLoaded(string? explicitPath)
    {
        if (_libraryHandle != IntPtr.Zero)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_libraryHandle != IntPtr.Zero)
            {
                return;
            }

            string? resolvedPath = null;

            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                resolvedPath = explicitPath;
                if (!System.IO.Path.IsPathRooted(resolvedPath))
                {
                    resolvedPath = System.IO.Path.Combine(AppContext.BaseDirectory, resolvedPath);
                }

                if (!System.IO.File.Exists(resolvedPath))
                {
                    throw new System.IO.FileNotFoundException($"NFIQ2 native library not found at '{resolvedPath}'.", resolvedPath);
                }
            }

            if (resolvedPath == null)
            {
                // When no explicit path is provided, rely on OS search paths.
                var candidate = System.IO.Path.Combine(AppContext.BaseDirectory, NativeLibraryName);
                if (System.IO.File.Exists(candidate))
                {
                    resolvedPath = candidate;
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _libraryHandle = Windows.LoadLibrary(resolvedPath ?? NativeLibraryName);
            }
            else
            {
                _libraryHandle = Posix.dlopen(resolvedPath ?? NativeLibraryName, Posix.RtldNow | Posix.RtldGlobal);
            }

            if (_libraryHandle == IntPtr.Zero)
            {
                throw new DllNotFoundException(
                    $"Unable to load NFIQ2 native library. Ensure {NativeLibraryName} is available or provide an explicit path.");
            }
        }
    }

    private const CallingConvention CallConv = CallingConvention.Winapi;

    [DllImport(LibraryBaseName, CallingConvention = CallConv)]
    internal static extern void GetNfiq2Version(out int major, out int minor, out int patch, out IntPtr openCvVersion);

    [DllImport(LibraryBaseName, CallingConvention = CallConv)]
    internal static extern IntPtr InitNfiq2(out IntPtr hashPointer);

    [DllImport(LibraryBaseName, CallingConvention = CallConv)]
    internal static extern int ComputeNfiq2Score(int fingerPosition, byte[] pixels, int size, int width, int height, int ppi);

    [DllImport(LibraryBaseName, CallingConvention = CallConv)]
    internal static extern void ShutdownNfiq2();

    [DllImport(LibraryBaseName, CallingConvention = CallConv)]
    internal static extern void FreeNfiq2Buffer(IntPtr buffer);

    [DllImport(LibraryBaseName, CallingConvention = CallConv)]
    internal static extern IntPtr GetLastNfiq2Error();

    private static class Windows
    {
        private const string Kernel32 = "kernel32";

        [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryW(string lpFileName);

        internal static IntPtr LoadLibrary(string path)
        {
            return LoadLibraryW(path);
        }
    }

    private static class Posix
    {
        private const string LibDl = "libdl";

        internal const int RtldNow = 0x0002;
        internal const int RtldGlobal = 0x0100;

        [DllImport(LibDl, EntryPoint = "dlopen", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr NativeDlopen(string fileName, int flags);

        internal static IntPtr dlopen(string? fileName, int flags)
        {
            if (fileName == null)
            {
                fileName = NativeLibraryName;
            }

            var handle = NativeDlopen(fileName, flags);
            return handle;
        }
    }
}
