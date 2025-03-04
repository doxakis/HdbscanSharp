using System.Numerics;
using System.Numerics.Tensors;

namespace HdbscanSharp.Distance;

/// <summary>
/// Computes cosine distance between two points, d = 1 - ((X*Y) / (||X||*||Y||))
/// </summary>
public class CosineSimilarity<T> : IDistanceCalculator<T>
    where T : IRootFunctions<T>
{
    public double ComputeDistance(T[] attributesOne, T[] attributesTwo)
    {
        var cosineSimilarity = TensorPrimitives.CosineSimilarity<T>(attributesOne, attributesTwo);
        return 1 - double.CreateTruncating(cosineSimilarity);
    }
}