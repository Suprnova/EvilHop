namespace EvilHop.Serialization.Validation;

public abstract partial class V3Validator : V2Validator
{
    protected internal V3Validator()
    {
    }
}

public partial class MovieValidator : V3Validator
{
    protected internal override FileFormatVersion Version => FileFormatVersion.Movie;
}

public partial class IncrediblesValidator : V3Validator
{
    protected internal override FileFormatVersion Version => FileFormatVersion.Incredibles;
}
