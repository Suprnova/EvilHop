using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.Immutable;

namespace EvilHop.Tests.Serialization;

public class AnimationListAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.AnimationList;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Ids(params uint[] ids)
    {
        byte[] result = new byte[40];
        for (int i = 0; i < 10; i++)
        {
            uint id = i < ids.Length ? ids[i] : 0;
            BitConverter.GetBytes(id).Reverse().ToArray().CopyTo(result, i * 4);
        }
        return result;
    }

    private static byte[] StateHashesAndPhysics(params uint[] hashes)
    {
        byte[] result = new byte[52];
        for (int i = 0; i < 10; i++)
        {
            uint hash = i < hashes.Length ? hashes[i] : 0;
            BitConverter.GetBytes(hash).Reverse().ToArray().CopyTo(result, i * 4);
        }
        return result;
    }

    [Fact]
    public void Read_AnimationList_ProducesAnimationListAsset() =>
        Assert.IsType<AnimationListAsset>(Read(Ids()));

    [Fact]
    public void Read_AnimationList_PopulatesIds()
    {
        var asset = (AnimationListAsset)Read(Ids(0x11111111, 0x22222222));

        Assert.Equal(10, asset.Ids.Length);
        Assert.Equal(new AssetId(0x11111111), asset.Ids[0]);
        Assert.Equal(new AssetId(0x22222222), asset.Ids[1]);
        Assert.Equal(AssetId.None, asset.Ids[2]);
    }

    [Fact]
    public void Read_ThenWrite_AnimationList_ReproducesInputBytes()
    {
        byte[] data = Ids(0x11111111, 0x22222222, 0x33333333);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_AnimationListWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Ids(0x11111111), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_AnimationList_UnderROTU_PopulatesStateHashesAndHasPhysics()
    {
        byte[] data = [.. Ids(0x11111111), .. StateHashesAndPhysics(0xAAAAAAAA, 0xBBBBBBBB)];
        var profile = ROTUSerializer.DefaultProfile;

        var asset = (AnimationListAsset)Read(data, profile);

        Assert.Equal(10, asset.StateHashes.Length);
        Assert.Equal(0xAAAAAAAAu, asset.StateHashes[0]);
        Assert.Equal(0xBBBBBBBBu, asset.StateHashes[1]);
        Assert.Equal(0u, asset.StateHashes[2]);
    }

    [Fact]
    public void Read_ThenWrite_AnimationListUnderROTU_ReproducesInputBytes()
    {
        byte[] data = [.. Ids(0x11111111, 0x22222222), .. StateHashesAndPhysics(0xAAAAAAAA, 0xBBBBBBBB)];
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_AnimationListUnderRatatouille_ReproducesInputBytes()
    {
        byte[] data = [.. Ids(0x11111111), .. StateHashesAndPhysics(0xAAAAAAAA)];
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_AnimationList_UnderBFBB_IgnoresBytesPastIds()
    {
        // BFBB doesn't carry StateHashes/HasPhysics; anything past the 10 ids is unparsed tail, not
        // misread as those fields.
        byte[] data = [.. Ids(0x11111111), .. StateHashesAndPhysics(0xAAAAAAAA)];

        var asset = (AnimationListAsset)Read(data);

        Assert.Equal(0u, asset.StateHashes[0]);
        Assert.Equal(52, asset.GetUnparsedTail().Length);
    }

    [Fact]
    public void Ids_AssignedWrongLength_ThrowsArgumentException()
    {
        var asset = new AnimationListAsset();

        Assert.Throws<ArgumentException>(() => asset.Ids = [.. new AssetId[9]]);
    }
}
