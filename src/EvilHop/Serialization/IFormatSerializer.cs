using EvilHop.Blocks;
using EvilHop.Serialization.Validation;

namespace EvilHop.Serialization;

public class SerializerOptions
{
    public ValidationMode Mode { get; set; } = ValidationMode.None;
    public Action<ValidationIssue>? OnValidationIssue { get; set; }
}

public interface IFormatSerializer
{
    /// <summary>
    /// Reads a <see cref="HipFile"/> from the <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/> to read from.</param>
    /// <returns>A <see cref="HipFile"/> read from this <see cref="BinaryReader"/>.</returns>
    HipFile ReadArchive(BinaryReader reader);

    /// <summary>
    /// Reads a <see cref="HipFile"/> from the <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/> to read from.</param>
    /// <param name="options">The <see cref="SerializerOptions"/> to use when reading.</param>
    /// <returns>A <see cref="HipFile"/> read from this <see cref="BinaryReader"/>.</returns>
    /// <exception cref="InvalidDataException">
    /// If the <see cref="HipFile"/> is invalid and <see cref="SerializerOptions.Mode"/> is set to <see cref="ValidationMode.Strict"/>.
    /// </exception>
    HipFile ReadArchive(BinaryReader reader, SerializerOptions? options = null);

    /// <summary>
    /// Writes a <see cref="HipFile"/> to the <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The <see cref="BinaryWriter"/> to write to.</param>
    /// <param name="archive">The <see cref="HipFile"/> to write.</param>
    void WriteArchive(BinaryWriter writer, HipFile archive);

    // todo: use a better exception for invalid block read?
    /// <summary>
    /// Reads a <see cref="Block"/> from the <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/> to read from.</param>
    /// <param name="options">The <see cref="SerializerOptions"/> to use when reading.</param>
    /// <returns>A <see cref="Block"/> read from this <see cref="BinaryReader"/>.</returns>
    /// <exception cref="InvalidDataException">
    /// If the <see cref="HipFile"/> is invalid and <see cref="SerializerOptions.Mode"/> is set to <see cref="ValidationMode.Strict"/>, 
    /// or if a <see cref="Block"/> of an unknown <see langword="type"/> is found.
    /// </exception>
    Block Read(BinaryReader reader, SerializerOptions? options = null);

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
    TBlock Read<TBlock>(BinaryReader reader, SerializerOptions? options = null) where TBlock : Block;

    /// <summary>
    /// Writes the <paramref name="block"/> to the <paramref name="writer"/>.
    /// </summary>
    /// <remarks>
    /// If a block does not have a valid writer method assigned to it, it is skipped.
    /// </remarks>
    /// <param name="writer">The <see cref="BinaryWriter"/> to write to.</param>
    /// <param name="block">The <see cref="Block"/> to write.</param>
    void Write(BinaryWriter writer, Block block);

    /// <summary>
    /// Ensures that the provided <paramref name="block"/> matches expected specifications.
    /// </summary>
    /// <param name="block">The <see cref="Block"/> to validate.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of any <see cref="ValidationIssue"/> found.</returns>
    IEnumerable<ValidationIssue> Validate(Block block);

    /// <summary>
    /// Ensures that the provided <paramref name="hip"/> matches expected specifications.
    /// </summary>
    /// <param name="hip">The <see cref="HipFile"/> to validate.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of any <see cref="ValidationIssue"/> found.</returns>
    IEnumerable<ValidationIssue> ValidateArchive(HipFile hip);

    /// <summary>
    /// Determines the size of the <paramref name="block"/> were it to be written by <see langword="this"/> <see cref="IFormatSerializer"/>.
    /// </summary>
    /// <param name="block">The <see cref="Block"/> to determine the size of.</param>
    /// <returns>The written size of the <paramref name="block"/>.</returns>
    uint GetBlockLength(Block block);
}
