using System;
using System.Numerics;
using System.Numerics.Tensors;

namespace HdbscanSharp.Distance;

/// <summary>
/// Computes the supremum distance between two points, d = max[(x1-y1), (x2-y2), ... ,(xn-yn)].
/// </summary>
public class SupremumDistance<T> : IDistanceCalculator<T>
	where T : unmanaged, INumber<T>
{
	public double ComputeDistance(T[] attributesOne, T[] attributesTwo)
	{
		Span<T> diff = stackalloc T[Math.Min(attributesOne.Length, attributesTwo.Length)];
		TensorPrimitives.Subtract(attributesOne, attributesTwo, diff);
		Span<T> abs = stackalloc T[diff.Length];
		TensorPrimitives.Abs(diff, abs);
		var sup = TensorPrimitives.Max<T>(abs);
		return double.CreateTruncating(sup);
	}
}