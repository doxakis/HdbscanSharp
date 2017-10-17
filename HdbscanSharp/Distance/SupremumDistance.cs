using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Distance
{
	/// <summary>
	/// Computes the supremum distance between two points, d = max[(x1-y1), (x2-y2), ... ,(xn-yn)].
	/// </summary>
	public class SupremumDistance : IDistanceCalculator
	{
		public double ComputeDistance(double[] attributesOne, double[] attributesTwo)
		{
			double distance = 0;
			for (int i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
			{
				double difference = Math.Abs(attributesOne[i] - attributesTwo[i]);
				if (difference > distance)
					distance = difference;
			}
			return distance;
		}
	}
}
