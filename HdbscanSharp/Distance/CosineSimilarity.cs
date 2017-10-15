using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Distance
{
	/**
	* Computes cosine similarity between two points, d = 1 - ((X*Y) / (||X||*||Y||))
	*/
	public class CosineSimilarity : IDistanceCalculator
	{
		public double ComputeDistance(double[] attributesOne, double[] attributesTwo)
		{
			double dotProduct = 0;
			double magnitudeOne = 0;
			double magnitudeTwo = 0;

			for (int i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
			{
				dotProduct += (attributesOne[i] * attributesTwo[i]);
				magnitudeOne += (attributesOne[i] * attributesOne[i]);
				magnitudeTwo += (attributesTwo[i] * attributesTwo[i]);
			}
			return 1 - (dotProduct / Math.Sqrt(magnitudeOne * magnitudeTwo));
		}
	}
}
