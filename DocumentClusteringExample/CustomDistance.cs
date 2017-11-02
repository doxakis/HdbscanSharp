using HdbscanSharp.Distance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentClusteringExample
{
	public class CustomDistance : IDistanceCalculator
	{
		public double ComputeDistance(double[] attributesOne, double[] attributesTwo)
		{
			double count = 0;
			double numA = 0;
			double numB = 0;

			for (int i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
			{
				if (attributesOne[i] > 0)
				{
					numA = attributesOne[i];
				}

				if (attributesTwo[i] > 0)
				{
					numB += attributesTwo[i];
				}

				if (attributesOne[i] > 0 && attributesTwo[i] > 0)
				{
					count += Math.Min(attributesOne[i], attributesTwo[i]);
				}
			}

			double denom = Math.Max(numA, numB);
			if (denom == 0)
			{
				return 0;
			}
			return 1.0 * count / denom;
		}
	}
}
