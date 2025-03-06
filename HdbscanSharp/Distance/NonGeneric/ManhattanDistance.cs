using System;

namespace HdbscanSharp.Distance
{
	/// <summary>
	/// Computes the manhattan distance between two points, d = |x1-y1| + |x2-y2| + ... + |xn-yn|.
	/// </summary>
	public class ManhattanDistance : IDistanceCalculator<double[]>
	{
		public double ComputeDistance(int indexOne, int indexTwo, double[] attributesOne, double[] attributesTwo)
		{
			double distance = 0;
			for (var i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
			{
				distance += Math.Abs(attributesOne[i] - attributesTwo[i]);
			}
			return distance;
		}
	}
}
