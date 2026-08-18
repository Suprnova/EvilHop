using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary>
/// Shared bookkeeping for the common boolean invariant shape: a <c>checked</c> total, per-outcome
/// counts, and up to 50 samples of the failing outcome. Passing checks don't get samples - a
/// satisfied invariant doesn't need a worked example, and the count alone already says everything
/// a sample would.
/// </summary>
internal sealed class InvariantResult
{
    private const int ViolatedSampleCap = 50;

    private long _violated;
    private long _passing;
    private readonly List<JsonObject> _violatedSamples = [];

    /// <summary>
    /// Records one checked outcome. <paramref name="sample"/> is invoked only for a failing outcome
    /// still under its cap.
    /// </summary>
    public void Record(bool passed, Func<JsonObject> sample)
    {
        if (passed)
        {
            _passing++;
        }
        else
        {
            _violated++;
            if (_violatedSamples.Count < ViolatedSampleCap) _violatedSamples.Add(sample());
        }
    }

    public JsonObject ToJson()
    {
        var result = new JsonObject
        {
            ["checked"] = _violated + _passing,
            ["outcomes"] = new JsonObject { ["violated"] = _violated, ["passing"] = _passing }
        };
        if (_violatedSamples.Count > 0) result["violated"] = new JsonArray([.. _violatedSamples]);
        return result;
    }
}
