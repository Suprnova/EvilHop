namespace EvilHop.Validation;

/// <summary>
/// How consequential a <see cref="ValidationIssue"/> is to the game that loads the archive.
/// </summary>
public enum Severity
{
    /// <summary>
    /// Purely observational. The value is odd, unexpected, or outside what shipped archives do,
    /// but it demonstrably has no effect on the game.
    /// </summary>
    Info,

    /// <summary>
    /// Could cause problems, the consequences are undocumented, or required additional
    /// configuration. Not expected to crash.
    /// </summary>
    Warning,

    /// <summary>
    /// Known to be unrecoverable. The game will fail to load the archive or will crash.
    /// </summary>
    Error
}
