using System;
using System.Numerics;
using System.Numerics.Tensors;

namespace HdbscanSharp.Distance;

/// <summary>
/// Computes the manhattan distance between two points, d = |x1-y1| + |x2-y2| + ... + |xn-yn|.
/// </summary>
public class ManhattanDistance<T> : IDistanceCalculator<T>
	where T : unmanaged, INumberBase<T>
{
	public double ComputeDistance(T[] attributesOne, T[] attributesTwo)
	{
		Span<T> diff = stackalloc T[Math.Min(attributesOne.Length, attributesTwo.Length)];
		TensorPrimitives.Subtract(attributesOne, attributesTwo, diff);
		var l1Norm = TensorPrimitives.SumOfMagnitudes<T>(diff);
		return double.CreateTruncating(l1Norm);
	}
}