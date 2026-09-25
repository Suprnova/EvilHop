using System.Globalization;

namespace EvilHop.Common;

/// <summary>
/// Single-bit edits on a <see cref="FlagsAttribute"/> enum, the setter counterpart to
/// <see cref="Enum.HasFlag(Enum)"/>. Used by logical <see cref="bool"/> properties that project one
/// bit of a physical flags field.
/// </summary>
internal static class FlagsExtensions
{
    extension<T>(T flags) where T : struct, Enum
    {
        /// <summary>
        /// Returns these flags with <paramref name="flag"/> set when <paramref name="value"/> is
        /// <see langword="true"/> and cleared otherwise, every other bit unchanged.
        /// </summary>
        public T WithFlag(T flag, bool value)
        {
            long bits = Convert.ToInt64(flags, CultureInfo.InvariantCulture);
            long bit = Convert.ToInt64(flag, CultureInfo.InvariantCulture);
            return (T)Enum.ToObject(typeof(T), value ? bits | bit : bits & ~bit);
        }
    }
}
