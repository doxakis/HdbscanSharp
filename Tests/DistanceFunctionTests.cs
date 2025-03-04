using System;
using HdbscanSharp.Distance;
using Xunit;

namespace Tests;

public class DistanceFunctionTests
{
    [Theory, ClassData(typeof(DistanceFunctionTestsData))]
    public void TestComputedDistanceIsAsExpected(
        float[] x,
        float[] y,
        double expectedDistance,
        IDistanceCalculator<float> calculator)
    {
        var actualDistance = calculator.ComputeDistance(x, y);
        var equal = Math.Abs(expectedDistance - actualDistance) < double.Epsilon;
        Assert.True(equal);
    }
}

public class DistanceFunctionTestsData : TheoryData<float[], float[], double, IDistanceCalculator<float>>
{
    public DistanceFunctionTestsData()
    {
        Add([1, 2, 3, 5, 8], [0.11f, 0.12f, 0.13f, 0.15f, 0.18f], 1 - 0.920814711f, new CosineSimilarity<float>());
        Add([0, 0, 0], [1, 2, 3], float.Sqrt(14), new EuclideanDistance<float>());
        Add([0, 0, 0], [1, 2, 3], 6, new ManhattanDistance<float>());
        Add([1, 2, 3, 5, 8], [0.11f, 0.12f, 0.13f, 0.15f, 0.18f], 0, new PearsonCorrelation<float>());
        Add([0, 0, 0], [1, 2, 3], 3, new SupremumDistance<float>());
    }
}