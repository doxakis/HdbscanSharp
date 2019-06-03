using System;

namespace HdbscanSharp.Distance
{
	/// <summary>
	/// Computes cosine similarity between two points, d = 1 - ((X*Y) / (||X||*||Y||))
	/// </summary>
	public class CosineSimilarity : IDistanceCalculator
	{
		public double ComputeDistance(double[] attributesOne, double[] attributesTwo)
		{
			double dotProduct = 0;
			double magnitudeOne = 0;
			double magnitudeTwo = 0;

			for (var i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
			{
				dotProduct += (attributesOne[i] * attributesTwo[i]);
				magnitudeOne += (attributesOne[i] * attributesOne[i]);
				magnitudeTwo += (attributesTwo[i] * attributesTwo[i]);
			}
			return Math.Max(0, 1 - (dotProduct / Math.Sqrt(magnitudeOne * magnitudeTwo)));
		}
	}
}
