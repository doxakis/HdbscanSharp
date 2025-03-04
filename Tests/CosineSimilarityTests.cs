using HdbscanSharp.Distance;
using Xunit;

namespace Tests;

public class CosineSimilarityTests
{
    [Fact]
    public void TestDistanceIsPositiveEvenIfThereIsRounding()
    {
        // See: https://github.com/doxakis/HdbscanSharp/issues/5

        var a = new double[] { 20 };
        var b = new double[] { 19.990000000000002 };

        var distFunc = new CosineSimilarity<double>();
        var distance = distFunc.ComputeDistance(a, b);
        if (distance < 0)
        {
            Assert.Fail("Distance must be positive.");
        }
    }
}