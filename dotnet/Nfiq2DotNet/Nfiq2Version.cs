namespace Nfiq2DotNet;

/// <summary>
/// Represents the version information returned by the native NFIQ2 library.
/// </summary>
public readonly struct Nfiq2Version
{
    /// <summary>Gets the major version number.</summary>
    public int Major { get; }
    /// <summary>Gets the minor version number.</summary>
    public int Minor { get; }
    /// <summary>Gets the patch version number.</summary>
    public int Patch { get; }
    /// <summary>Gets the OpenCV version string reported by the native library.</summary>
    public string OpenCvVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Nfiq2Version"/> structure.
    /// </summary>
    /// <param name="major">Major version number.</param>
    /// <param name="minor">Minor version number.</param>
    /// <param name="patch">Patch version number.</param>
    /// <param name="openCvVersion">OpenCV version string.</param>
    public Nfiq2Version(int major, int minor, int patch, string openCvVersion)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        OpenCvVersion = openCvVersion;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Major}.{Minor}.{Patch} (OpenCV {OpenCvVersion})";
}
