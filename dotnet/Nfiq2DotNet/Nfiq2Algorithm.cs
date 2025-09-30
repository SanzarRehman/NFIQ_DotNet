using System;
using System.Runtime.InteropServices;
using Nfiq2DotNet.Internal;

namespace Nfiq2DotNet;

/// <summary>
/// High level managed wrapper around the native NFIQ2 algorithm implementation.
/// </summary>
public sealed class Nfiq2Algorithm : IDisposable
{
    private readonly object _syncRoot = new object();
    private bool _initialized;
    private string? _parameterHash;
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether the underlying native algorithm has been initialised.
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Gets the parameter hash returned by the native library during initialisation.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the algorithm has not yet been initialised.</exception>
    public string ParameterHash => _parameterHash ?? throw new InvalidOperationException("NFIQ2 is not initialised. Call Initialise() first.");

    /// <summary>
    /// Initializes the native NFIQ2 algorithm.
    /// </summary>
    /// <param name="nativeLibraryPath">Optional path to the NFIQ2 native library (Nfiq2Api.dll / libNfiq2Api.so).</param>
    /// <returns>The parameter hash reported by the native model.</returns>
    public string Initialise(string? nativeLibraryPath = null)
    {
        ThrowIfDisposed();

        lock (_syncRoot)
        {
            if (_initialized)
            {
                return _parameterHash!;
            }

            Nfiq2Native.EnsureLibraryLoaded(nativeLibraryPath);

            IntPtr hashPointer;
            var returnPointer = Nfiq2Native.InitNfiq2(out hashPointer);
            if (returnPointer == IntPtr.Zero)
            {
                ThrowLastError("InitNfiq2");
            }

            try
            {
                _parameterHash = Marshal.PtrToStringAnsi(returnPointer) ?? string.Empty;
            }
            finally
            {
                if (hashPointer != IntPtr.Zero)
                {
                    Nfiq2Native.FreeNfiq2Buffer(hashPointer);
                }
            }

            _initialized = true;
            return _parameterHash;
        }
    }

    /// <summary>
    /// Computes the NFIQ2 quality score for a grayscale fingerprint image.
    /// </summary>
    /// <param name="pixels">Fingerprint image data (8-bit grayscale) laid out row-major.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="fingerPosition">Finger position code (see <see cref="FingerPosition"/>).</param>
    /// <param name="ppi">Pixels per inch resolution. Defaults to 500, the most common setting.</param>
    /// <returns>The quality score (0-100 where 100 indicates highest quality).</returns>
    public int ComputeScore(byte[] pixels, int width, int height, FingerPosition fingerPosition = FingerPosition.Unknown, int ppi = 500)
    {
        ThrowIfDisposed();

        if (pixels == null)
        {
            throw new ArgumentNullException(nameof(pixels));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
        }

        if (ppi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ppi), ppi, "PPI must be positive.");
        }

        // Validate buffer length matches expected pixel count (8 bpp grayscale).
        var expectedLength = checked(width * (long)height);
        if (pixels.LongLength < expectedLength)
        {
            throw new ArgumentException($"Pixel buffer length ({pixels.LongLength}) is smaller than width*height ({expectedLength}).", nameof(pixels));
        }

        EnsureInitialised();

        var score = Nfiq2Native.ComputeNfiq2Score((int)fingerPosition, pixels, pixels.Length, width, height, ppi);
        if (score == 255 || score < 0)
        {
            ThrowLastError("ComputeNfiq2Score");
        }

        return score;
    }

    /// <summary>
    /// Retrieves the version information exposed by the native library.
    /// </summary>
    public static Nfiq2Version GetVersion(string? nativeLibraryPath = null)
    {
        Nfiq2Native.EnsureLibraryLoaded(nativeLibraryPath);

        Nfiq2Native.GetNfiq2Version(out var major, out var minor, out var patch, out var openCvPtr);
        var openCvVersion = openCvPtr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(openCvPtr) ?? string.Empty;
        return new Nfiq2Version(major, minor, patch, openCvVersion);
    }

    /// <summary>
    /// Releases the native NFIQ2 resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            lock (_syncRoot)
            {
                if (_initialized)
                {
                    Nfiq2Native.ShutdownNfiq2();
                    _initialized = false;
                }
            }
        }

        _disposed = true;
    }

    private void EnsureInitialised()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("NFIQ2 has not been initialised. Call Initialise() before computing scores.");
        }
    }

    private static void ThrowLastError(string operation)
    {
        var messagePtr = Nfiq2Native.GetLastNfiq2Error();
        var message = messagePtr == IntPtr.Zero ? $"{operation} failed with an unknown error." : Marshal.PtrToStringAnsi(messagePtr) ?? $"{operation} failed with an unknown error.";
        throw new Nfiq2Exception(message);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Nfiq2Algorithm));
        }
    }
}
