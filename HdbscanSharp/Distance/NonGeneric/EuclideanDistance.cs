using System;

namespace HdbscanSharp.Distance
{
	/// <summary>
	/// Computes the euclidean distance between two points, d = sqrt((x1-y1)^2 + (x2-y2)^2 + ... + (xn-yn)^2).
	/// </summary>
	public class EuclideanDistance : IDistanceCalculator<double[]>
	{
		public double ComputeDistance(int indexOne, int indexTwo, double[] attributesOne, double[] attributesTwo)
		{
			double distance = 0;
			for (var i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
			{
				distance += ((attributesOne[i] - attributesTwo[i]) * (attributesOne[i] - attributesTwo[i]));
			}
			return Math.Sqrt(distance);
		}
	}
}
