using System.Numerics;
using System.Numerics.Tensors;

namespace HdbscanSharp.Distance;

/// <summary>
/// Computes the euclidean distance between two points, d = sqrt((x1-y1)^2 + (x2-y2)^2 + ... + (xn-yn)^2).
/// </summary>
public class EuclideanDistance<T> : IDistanceCalculator<T>
	where T : IRootFunctions<T>
{
	public double ComputeDistance(T[] attributesOne, T[] attributesTwo)
	{
		var l2Norm = TensorPrimitives.Distance<T>(attributesOne, attributesTwo);
		return double.CreateTruncating(l2Norm);
	}
}