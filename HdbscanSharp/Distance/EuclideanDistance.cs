using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Distance
{
	/**
	 * Computes the euclidean distance between two points, d = sqrt((x1-y1)^2 + (x2-y2)^2 + ... + (xn-yn)^2).
	 */
	public class EuclideanDistance : IDistanceCalculator
	{
		public double ComputeDistance(double[] attributesOne, double[] attributesTwo)
		{
			double distance = 0;
			for (int i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
			{
				distance += ((attributesOne[i] - attributesTwo[i]) * (attributesOne[i] - attributesTwo[i]));
			}
			return Math.Sqrt(distance);
		}
	}
}
