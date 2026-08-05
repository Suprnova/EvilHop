using EvilHop.Blocks;
using EvilHop.Serialization;
using EvilHop.Serialization.Validation;

namespace EvilHop.Tests.Blocks;

public class HIPATests
{
    // todo: put these in a helper class to share across all tests
    private static readonly IFormatSerializer _scoobyPrototype = FileFormatFactory.GetSerializer(FileFormatVersion.ScoobyPrototype);
    private static readonly IFormatSerializer _scooby = FileFormatFactory.GetSerializer(FileFormatVersion.Scooby);

    private static readonly IFormatSerializer[] serializers = [_scoobyPrototype, _scooby];

    private static readonly byte[] validHIPA = [0x48, 0x49, 0x50, 0x41, 0x00, 0x00, 0x00, 0x00];

    private static string FormatValidationIssues(IEnumerable<ValidationIssue> issues)
    {
        return string.Join("; ", issues.Select(i => $"[{i.Severity}] {i.Message}"));
    }

    [Fact]
    public void EmptyConstructor_IsValid()
    {
        HIPA hipa = new();

        foreach (var s in serializers)
        {
            Assert.True(hipa.IsValid(s, out var issues), 
                $"HIPA validation failed for {s.GetType().Name}. Issues: {FormatValidationIssues(issues)}");
        }
    }

    [Fact]
    public void IFormatSerializer_New_IsValid()
    {
        foreach (var s in serializers)
        {
            var hipa = s.NewBlock<HIPA>();
            Assert.True(hipa.IsValid(s, out var issues), 
                $"HIPA validation failed for {s.GetType().Name}. Issues: {FormatValidationIssues(issues)}");
        }
    }

    [Fact]
    public void IFormatSerializer_Read_ValidBytes_IsValid()
    {
        using BinaryReader reader = new(new MemoryStream(validHIPA));

        foreach (var s in serializers)
        {
            reader.BaseStream.Position = 0;
            var hipa = s.ReadBlock<HIPA>(reader);
            Assert.True(hipa.IsValid(s, out var issues), 
                $"HIPA validation failed for {s.GetType().Name}. Issues: {FormatValidationIssues(issues)}");
        }
    }

    [Fact]
    public void IFormatSerializer_Read_ValidBytes_CorrectOffset()
    {
        using BinaryReader reader = new(new MemoryStream(validHIPA));

        foreach (var s in serializers)
        {
            reader.BaseStream.Position = 0;
            _ = s.ReadBlock<HIPA>(reader);
            Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
        }
    }

    [Fact]
    public void IFormatSerializer_WriteFromNew_Matches()
    {
        foreach (var s in serializers)
        {
            byte[] writeBytes = new byte[validHIPA.Length];
            using BinaryWriter writer = new(new MemoryStream(writeBytes));

            s.WriteBlock(writer, s.NewBlock<HIPA>());
            Assert.Equal(validHIPA, writeBytes);
        }
    }

    [Fact]
    public void IFormatSerializer_WriteFromRead_Matches()
    {
        using BinaryReader reader = new(new MemoryStream(validHIPA));

        foreach (var s in serializers)
        {
            byte[] writeBytes = new byte[validHIPA.Length];
            using BinaryWriter writer = new(new MemoryStream(writeBytes));
            reader.BaseStream.Position = 0;

            HIPA hipa = s.ReadBlock<HIPA>(reader);
            s.WriteBlock(writer, hipa);
            Assert.Equal(validHIPA, writeBytes);
        }
    }
}
