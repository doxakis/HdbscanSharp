using System;
using System.Numerics;
using System.Numerics.Tensors;

namespace HdbscanSharp.Distance;

/// <summary>
/// Computes the Pearson distance between two points, d = 1 - (cov(X,Y) / (std_dev(X) * std_dev(Y)))
/// </summary>
public class PearsonCorrelation<T> : IDistanceCalculator<T>
    where T : unmanaged, IRootFunctions<T>, INumber<T>, IDivisionOperators<T, T, T>
{
    public double ComputeDistance(T[] attributesOne, T[] attributesTwo)
    {
        var meanOne = TensorPrimitives.Sum<T>(attributesOne) / T.CreateTruncating(attributesOne.Length);
        var meanTwo = TensorPrimitives.Sum<T>(attributesTwo) / T.CreateTruncating(attributesTwo.Length);

        Span<T> diff1 = stackalloc T[attributesOne.Length];
        TensorPrimitives.Subtract(attributesOne, meanOne, diff1);

        Span<T> diff2 = stackalloc T[attributesTwo.Length];
        TensorPrimitives.Subtract(attributesTwo, meanTwo, diff2);

        Span<T> diff1TimesDiff2 = stackalloc T[Math.Min(diff1.Length, diff2.Length)];
        TensorPrimitives.Multiply(diff1, diff2, diff1TimesDiff2);
        var covariance = TensorPrimitives.Sum<T>(diff1TimesDiff2);

        var standardDeviationOne = TensorPrimitives.SumOfSquares<T>(diff1);
        var standardDeviationTwo = TensorPrimitives.SumOfSquares<T>(diff2);

        return double.CreateTruncating(
            T.Max(
                T.AdditiveIdentity,
                T.MultiplicativeIdentity - covariance / T.Sqrt(standardDeviationOne * standardDeviationTwo)));
    }
}