using HdbscanSharp.Distance;
using Xunit;

namespace Tests;

public class CosineSimilarityTests
{
    [Fact]
    public void TestDistanceIsPositiveEvenIfThereIsRounding()
    {
        // See: https://github.com/doxakis/HdbscanSharp/issues/5

        double[] a = [20];
        double[] b = [19.990000000000002];

        var distFunc = new CosineSimilarity<double>();
        var distance = distFunc.ComputeDistance(a, b);
        if (distance < 0)
        {
            Assert.Fail("Distance must be positive.");
        }
    }
}