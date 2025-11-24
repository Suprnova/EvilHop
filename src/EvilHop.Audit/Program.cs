using EvilHop;
using EvilHop.Serialization;

string ARTIFACTS_FOLDER = Path.GetFullPath("../../../../../artifacts");
string US_GC_RELEASE = Path.Combine("release", "GC", "NTSC-U", "US");

string SCOOBY_GC_FOLDER = Path.Combine(ARTIFACTS_FOLDER, "n100f", US_GC_RELEASE);

SerializerOptions options = new()
{
    Mode = EvilHop.Serialization.Validation.ValidationMode.Warn,
    OnValidationIssue = (i) => Console.WriteLine($"[{i.Severity}] - {i.Message}")
};

string[] scoobyFiles = Directory.GetFiles(SCOOBY_GC_FOLDER, "*.HIP", SearchOption.AllDirectories);

foreach (string file in scoobyFiles)
{
    Console.WriteLine(Path.GetFileName(file));
    using BinaryReader reader = new(File.Open(file, FileMode.Open));
    FileFormatVersion fileVersion = FileFormatFactory.SniffVersion(reader);
    IFormatSerializer serializer = FileFormatFactory.GetSerializer(fileVersion);

    HipFile archive = serializer.ReadArchive(reader, options);
}
