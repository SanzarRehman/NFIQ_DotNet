using System.Text;
using Nfiq2DotNet;

var options = ParseArguments(args);
if (options.ShowHelp)
{
    PrintUsage();
    return 1;
}

if (options.ImagePath is null)
{
    Console.Error.WriteLine("Error: an input fingerprint image path must be supplied.");
    PrintUsage();
    return 1;
}

if (!File.Exists(options.ImagePath))
{
    Console.Error.WriteLine($"Error: file '{options.ImagePath}' does not exist.");
    return 1;
}

try
{
    var image = LoadPgm(options.ImagePath);

    using var algorithm = new Nfiq2Algorithm();
    var hash = algorithm.Initialise(options.NativeLibraryPath);

    var score = algorithm.ComputeScore(
        image.Pixels,
        image.Width,
        image.Height,
        options.FingerPosition,
        options.Ppi);

    Console.WriteLine($"Computed NFIQ2 score: {score}");
    Console.WriteLine($"Parameter hash:      {hash}");
    Console.WriteLine($"Image size:          {image.Width}x{image.Height} @ {options.Ppi} PPI");
    Console.WriteLine($"Finger position:     {options.FingerPosition}");

    var version = Nfiq2Algorithm.GetVersion(options.NativeLibraryPath);
    Console.WriteLine($"Library version:     {version}");
}
catch (Nfiq2Exception ex)
{
    Console.Error.WriteLine($"NFIQ2 error: {ex.Message}");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex}");
    return 3;
}

return 0;

static void PrintUsage()
{
    Console.WriteLine("NFIQ2 .NET sample");
    Console.WriteLine("Usage: dotnet run --project dotnet/Nfiq2DotNet.Sample -- <fingerprint.pgm> [--finger=RightIndex] [--ppi=500] [--lib=/path/to/Nfiq2Api.dll]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  fingerprint.pgm   Path to an 8-bit grayscale PGM (P5) fingerprint image.");
    Console.WriteLine("  --finger=<value>  Optional finger position (defaults to Unknown).");
    Console.WriteLine("  --ppi=<value>     Optional pixels-per-inch (defaults to 500).");
    Console.WriteLine("  --lib=<path>      Optional explicit path to the native Nfiq2Api library.");
}

static CliOptions ParseArguments(string[] arguments)
{
    var result = new CliOptions();
    foreach (var argument in arguments)
    {
        if (argument is "-h" or "--help" or "/?")
        {
            result.ShowHelp = true;
            continue;
        }

        if (argument.StartsWith("--finger="))
        {
            var value = argument.Substring("--finger=".Length);
            if (Enum.TryParse<FingerPosition>(value, ignoreCase: true, out var finger))
            {
                result.FingerPosition = finger;
            }
            else
            {
                Console.Error.WriteLine($"Warning: unknown finger position '{value}'. Using Unknown.");
            }
            continue;
        }

        if (argument.StartsWith("--ppi="))
        {
            var value = argument.Substring("--ppi=".Length);
            if (!int.TryParse(value, out var ppi) || ppi <= 0)
            {
                Console.Error.WriteLine($"Warning: invalid PPI '{value}'. Using default {result.Ppi}.");
            }
            else
            {
                result.Ppi = ppi;
            }
            continue;
        }

        if (argument.StartsWith("--lib="))
        {
            result.NativeLibraryPath = argument.Substring("--lib=".Length);
            continue;
        }

        if (result.ImagePath is null)
        {
            result.ImagePath = argument;
            continue;
        }

        Console.Error.WriteLine($"Warning: ignoring unexpected argument '{argument}'.");
    }

    return result;
}

static FingerprintImage LoadPgm(string path)
{
    using var stream = File.OpenRead(path);
    using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: false);

    var magic = ReadToken(reader);
    if (!string.Equals(magic, "P5", StringComparison.Ordinal))
    {
        throw new InvalidDataException($"Unsupported PGM format '{magic}'. Only binary P5 images are supported.");
    }

    var width = int.Parse(ReadToken(reader));
    var height = int.Parse(ReadToken(reader));
    var maxValue = int.Parse(ReadToken(reader));

    if (maxValue > 255)
    {
        throw new InvalidDataException("Only 8-bit grayscale PGM images are supported (max value must be 255 or less).");
    }

    var expectedLength = checked(width * height);
    var pixels = reader.ReadBytes(expectedLength);
    if (pixels.Length != expectedLength)
    {
        throw new InvalidDataException($"PGM file truncated. Expected {expectedLength} bytes of raster data but read {pixels.Length}.");
    }

    return new FingerprintImage(width, height, pixels);
}

static string ReadToken(BinaryReader reader)
{
    var sb = new StringBuilder();
    var stream = reader.BaseStream;
    int b;

    // Skip whitespace and comments
    while (true)
    {
        b = stream.ReadByte();
        if (b == -1)
        {
            throw new EndOfStreamException("Unexpected end of file while parsing PGM header.");
        }

        var c = (char)b;
        if (char.IsWhiteSpace(c))
        {
            continue;
        }

        if (c == '#')
        {
            // Skip until end of line
            while (true)
            {
                b = stream.ReadByte();
                if (b == -1 || b == '\n')
                {
                    break;
                }
            }
            continue;
        }

        sb.Append(c);
        break;
    }

    // Consume characters until next whitespace
    while (true)
    {
        b = stream.ReadByte();
        if (b == -1)
        {
            break;
        }

        var c = (char)b;
        if (char.IsWhiteSpace(c))
        {
            break;
        }

        sb.Append(c);
    }

    return sb.ToString();
}

internal sealed record CliOptions
{
    public bool ShowHelp { get; set; }
    public string? ImagePath { get; set; }
    public FingerPosition FingerPosition { get; set; } = FingerPosition.Unknown;
    public int Ppi { get; set; } = 500;
    public string? NativeLibraryPath { get; set; }
}

internal sealed record FingerprintImage(int Width, int Height, byte[] Pixels);
