namespace Nfiq2DotNet;

/// <summary>
/// ISO/IEC 19794-4:2011 finger position codes used by NFIQ2.
/// </summary>
public enum FingerPosition : byte
{
    /// <summary>Finger position unspecified.</summary>
    Unknown = 0,
    /// <summary>Right hand thumb.</summary>
    RightThumb = 1,
    /// <summary>Right hand index finger.</summary>
    RightIndex = 2,
    /// <summary>Right hand middle finger.</summary>
    RightMiddle = 3,
    /// <summary>Right hand ring finger.</summary>
    RightRing = 4,
    /// <summary>Right hand little finger.</summary>
    RightLittle = 5,
    /// <summary>Left hand thumb.</summary>
    LeftThumb = 6,
    /// <summary>Left hand index finger.</summary>
    LeftIndex = 7,
    /// <summary>Left hand middle finger.</summary>
    LeftMiddle = 8,
    /// <summary>Left hand ring finger.</summary>
    LeftRing = 9,
    /// <summary>Left hand little finger.</summary>
    LeftLittle = 10,
    /// <summary>Right hand four-finger slap.</summary>
    RightFourFingerSlap = 13,
    /// <summary>Left hand four-finger slap.</summary>
    LeftFourFingerSlap = 14,
    /// <summary>Two thumbs captured simultaneously.</summary>
    TwoThumbs = 15
}
