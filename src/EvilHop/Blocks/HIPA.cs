using EvilHop.Blocks;

namespace EvilHop.Blocks
{
    public class HIPA : Block
    {
        protected internal override string Id => "HIPA";
        protected override uint DataLength => 0;
        public HIPA()
        {
        }
    }
}

namespace EvilHop.Serialization.Validation
{
    public partial class V1Validator
    {
        protected virtual IEnumerable<ValidationIssue> ValidateHIPA(HIPA hipa)
        {
            yield break;
        }
    }
}

