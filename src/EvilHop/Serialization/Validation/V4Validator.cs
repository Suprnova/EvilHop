namespace EvilHop.Serialization.Validation;

public abstract partial class V4Validator : V3Validator
{
    protected internal V4Validator()
    {
    }
}

public partial class ROTUValidator : V4Validator
{
}

public partial class RatValidator : V4Validator
{
}
