using EvilHop.Blocks;

namespace EvilHop.Blocks
{
    public class HIPA : Block
    {
        protected internal override string Id => "HIPA";
        protected internal override uint DataLength => 0;
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
            // todo: validate PCNT fields against AHDR, LHDR, and DPAK

            // todo: validate AHDR against STRM (?)

            // todo: validate ADBG checksum against STRM
            yield break;
        }
    }
}
