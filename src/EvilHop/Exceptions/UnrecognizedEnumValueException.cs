using System;
using System.Collections.Generic;
using System.Text;

namespace EvilHop.Exceptions;

/// <summary>
/// Initializes a new instance of the <see cref="UnrecognizedEnumValueException{TEnum}"/> class with the name of
/// the parameter that caused the exception and the actual value of the parameter.
/// </summary>
/// <typeparam name="TEnum">The type of enum attempted to convert into.</typeparam>
/// <param name="paramName">The name of the parameter that caused the exception.</param>
/// <param name="value">The actual value of the parameter.</param>
public class UnrecognizedEnumValueException<TEnum>(string? paramName, TEnum value)
    : ArgumentOutOfRangeException(paramName, $"Unrecognized value '{value}' for '{typeof(TEnum).Name}'.")
{
}
