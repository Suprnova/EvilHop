using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Serialization.Validation;

namespace EvilHop.Serialization;

public class SerializerOptions
{
    public ValidationMode Mode { get; set; } = ValidationMode.None;
    public Action<ValidationIssue>? OnValidationIssue { get; set; } // make event?
}

public interface IFormatSerializer
{
    /// <summary>
    /// Initializes a new <see cref="HipFile"/> with defaults appropriate for <see langword="this"/> <see cref="IFormatSerializer"/>.
    /// </summary>
    /// <returns>A <see cref="HipFile"/> with default values.</returns>
    HipFile NewHip();

    /// <summary>
    /// Reads a <see cref="HipFile"/> from the <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/> to read from.</param>
    /// <param name="options">The <see cref="SerializerOptions"/> to use when reading.</param>
    /// <returns>A <see cref="HipFile"/> read from this <see cref="BinaryReader"/>.</returns>
    /// <exception cref="InvalidDataException">
    /// If the <see cref="HipFile"/> is invalid and <see cref="SerializerOptions.Mode"/> is set to <see cref="ValidationMode.Strict"/>.
    /// </exception>
    HipFile ReadHip(BinaryReader reader, SerializerOptions? options = null);

    /// <summary>
    /// Writes a <see cref="HipFile"/> to the <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The <see cref="BinaryWriter"/> to write to.</param>
    /// <param name="archive">The <see cref="HipFile"/> to write.</param>
    void WriteHip(BinaryWriter writer, HipFile archive);

    /// <summary>
    /// Returns the size of the provided <paramref name="archive"/> were it to be written by this serializer.
    /// </summary>
    /// <param name="archive">The <see cref="HipFile"/> to determine the size of.</param>
    /// <returns>The size of the provided <paramref name="archive"/>.</returns>
    uint GetHipSize(HipFile archive);

    /// <summary>
    /// Ensures that the provided <paramref name="hip"/> matches expected specifications.
    /// </summary>
    /// <param name="hip">The <see cref="HipFile"/> to validate.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of any <see cref="ValidationIssue"/> found.</returns>
    IEnumerable<ValidationIssue> ValidateHip(HipFile hip);

    /// <summary>
    /// Initializes a <see cref="Block"/> of type <typeparamref name="TBlock"/> with defaults appropriate for <see langword="this"/> <see cref="IFormatSerializer"/>.
    /// </summary>
    /// <typeparam name="TBlock">The type of <see cref="Block"/> to initialize.</typeparam>
    /// <returns>A <typeparamref name="TBlock"/> with default values.</returns>
    TBlock NewBlock<TBlock>() where TBlock : Block;

    // todo: use a better exception for invalid block read?
    /// <summary>
    /// Reads a <see cref="Block"/> from the <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/> to read from.</param>
    /// <param name="options">The <see cref="SerializerOptions"/> to use when reading.</param>
    /// <returns>A <see cref="Block"/> read from this <see cref="BinaryReader"/>.</returns>
    /// <exception cref="InvalidDataException">
    /// If the <see cref="Block"/> is invalid and <see cref="SerializerOptions.Mode"/> is set to <see cref="ValidationMode.Strict"/>, 
    /// or if a <see cref="Block"/> of an unknown <see langword="type"/> is found.
    /// </exception>
    Block ReadBlock(BinaryReader reader, SerializerOptions? options = null);

    /// <summary>
    /// Reads a <see cref="Block"/> of type <typeparamref name="TBlock"/> from the <paramref name="reader"/>.
    /// </summary>
    /// <typeparam name="TBlock">The type of <see cref="Block"/> to read.</typeparam>
    /// <param name="reader">The <see cref="BinaryReader"/> to read from.</param>
    /// <param name="options">The <see cref="SerializerOptions"/> to use when reading.</param>
    /// <returns>A <see cref="Block"/> of type <typeparamref name="TBlock"/> read from this <see cref="BinaryReader"/>.</returns>
    /// <exception cref="InvalidDataException">
    /// If the <see cref="Block"/> is invalid and <see cref="SerializerOptions.Mode"/> is set to <see cref="ValidationMode.Strict"/>.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// If the <see cref="Block"/> read is not of type <typeparamref name="TBlock"/>.
    /// </exception>
    TBlock ReadBlock<TBlock>(BinaryReader reader, SerializerOptions? options = null) where TBlock : Block;

    /// <summary>
    /// Writes the <paramref name="block"/> to the <paramref name="writer"/>.
    /// </summary>
    /// <remarks>
    /// If a block does not have a valid writer method assigned to it, it is skipped.
    /// </remarks>
    /// <param name="writer">The <see cref="BinaryWriter"/> to write to.</param>
    /// <param name="block">The <see cref="Block"/> to write.</param>
    void WriteBlock(BinaryWriter writer, Block block);

    /// <summary>
    /// Returns the size of the provided <paramref name="block"/> were it to be written by this serializer.
    /// </summary>
    /// <param name="block">The <see cref="Block"/> to determine the size of.</param>
    /// <returns>The size of the provided <paramref name="block"/>.</returns>
    uint GetBlockSize(Block block);

    /// <summary>
    /// Ensures that the provided <paramref name="block"/> matches expected specifications.
    /// </summary>
    /// <param name="block">The <see cref="Block"/> to validate.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of any <see cref="ValidationIssue"/> found.</returns>
    IEnumerable<ValidationIssue> ValidateBlock(Block block);

    /// <summary>
    /// Initializes an <see cref="Asset"/> of type <typeparamref name="TAsset"/> with defaults appropriate for <see langword="this"/> <see cref="IFormatSerializer"/>.
    /// </summary>
    /// <typeparam name="TAsset">The type of <see cref="Asset"/> to initialize.</typeparam>
    /// <returns>A <typeparamref name="TAsset"/> with default values.</returns>
    TAsset NewAsset<TAsset>() where TAsset : Asset;

    /// <summary>
    /// Reads an <see cref="Asset"/> from the reader.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/> to read from.</param>
    /// <param name="options">The <see cref="SerializerOptions"/> to use when reading.</param>
    /// <returns>An <see cref="Asset"/> read from this <see cref="BinaryReader"/>.</returns>
    /// <exception cref="InvalidDataException">
    /// If the <see cref="Asset"/> is invalid and <see cref="SerializerOptions.Mode"/> is set to <see cref="ValidationMode.Strict"/>.
    /// </exception>
    Asset ReadAsset(BinaryReader reader, SerializerOptions? options = null);

    /// <summary>
    /// Reads an <see cref="Asset"/> of type <typeparamref name="TAsset"/> from the <paramref name="reader"/>.
    /// </summary>
    /// <typeparam name="TAsset">The type of <see cref="Asset"/> to read.</typeparam>
    /// <param name="reader">The <see cref="BinaryReader"/> to read from.</param>
    /// <param name="options">The <see cref="SerializerOptions"/> to use when reading.</param>
    /// <returns>An <see cref="Asset"/> of type <typeparamref name="TAsset"/> read from this <see cref="BinaryReader"/>.</returns>
    /// <exception cref="InvalidDataException">
    /// If the <see cref="Asset"/> is invalid and <see cref="SerializerOptions.Mode"/> is set to <see cref="ValidationMode.Strict"/>.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// If the <see cref="Asset"/> read is not of type <typeparamref name="TAsset"/>.
    /// </exception>
    TAsset ReadAsset<TAsset>(BinaryReader reader, SerializerOptions? options = null) where TAsset : Asset;

    /// <summary>
    /// Writes the <paramref name="asset"/> to the <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The <see cref="BinaryWriter"/> to write to.</param>
    /// <param name="asset">The <see cref="Asset"/> to write.</param>
    void WriteAsset(BinaryWriter writer, Asset asset);

    /// <summary>
    /// Ensures that the provided <paramref name="asset"/> matches expected specifications.
    /// </summary>
    /// <param name="asset">The <see cref="Asset"/> to validate.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of any <see cref="ValidationIssue"/> found.</returns>
    IEnumerable<ValidationIssue> ValidateAsset(Asset asset);
}
