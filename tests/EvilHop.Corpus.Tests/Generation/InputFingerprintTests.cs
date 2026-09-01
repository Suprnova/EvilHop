using EvilHop.Corpus.Generation;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Tests.Generation;

public class InputFingerprintTests
{
    private sealed class FakeFacetGenerator(params string[] dependencies) : IFacetGenerator
    {
        public string Id => "fake";
        public int Revision => 1;
        public IEnumerable<string> Dependencies => dependencies;
        public JsonObject Map(Archive archive) => [];
        public JsonObject Reduce(IReadOnlyList<MappedArchive> records) => [];
    }

    [Fact]
    public void InputFingerprint_CalledTwice_ReturnsSameValue()
    {
        IFacetGenerator generator = new FakeFacetGenerator("PVER.subVersion", "PVER.compatVersion");

        Assert.Equal(generator.InputFingerprint(), generator.InputFingerprint());
    }

    [Fact]
    public void InputFingerprint_DependencyOrderDoesNotMatter()
    {
        IFacetGenerator forward = new FakeFacetGenerator("PVER.subVersion", "PVER.compatVersion");
        IFacetGenerator backward = new FakeFacetGenerator("PVER.compatVersion", "PVER.subVersion");

        Assert.Equal(forward.InputFingerprint(), backward.InputFingerprint());
    }

    [Fact]
    public void InputFingerprint_DifferentDependencySets_ReturnDifferentFingerprints()
    {
        IFacetGenerator subVersionOnly = new FakeFacetGenerator("PVER.subVersion");
        IFacetGenerator both = new FakeFacetGenerator("PVER.subVersion", "PVER.compatVersion");

        Assert.NotEqual(subVersionOnly.InputFingerprint(), both.InputFingerprint());
    }

    [Fact]
    public void InputFingerprint_UnknownObservable_Throws()
    {
        IFacetGenerator generator = new FakeFacetGenerator("NOPE.doesNotExist");

        Assert.Throws<ArgumentException>(() => generator.InputFingerprint());
    }
}
