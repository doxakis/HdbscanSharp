using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Distance
{
	/// <summary>
	/// Computes the euclidean distance between two points, d = 1 - (cov(X,Y) / (std_dev(X) * std_dev(Y)))
	/// </summary>
	public class PearsonCorrelation : IDistanceCalculator
	{
		public double ComputeDistance(double[] attributesOne, double[] attributesTwo)
		{
			double meanOne = 0;
			double meanTwo = 0;
			for (int i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
			{
				meanOne += attributesOne[i];
				meanTwo += attributesTwo[i];
			}
			meanOne = meanOne / attributesOne.Length;
			meanTwo = meanTwo / attributesTwo.Length;
			double covariance = 0;
			double standardDeviationOne = 0;
			double standardDeviationTwo = 0;
			for (int i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
			{
				covariance += ((attributesOne[i] - meanOne) * (attributesTwo[i] - meanTwo));
				standardDeviationOne += ((attributesOne[i] - meanOne) * (attributesOne[i] - meanOne));
				standardDeviationTwo += ((attributesTwo[i] - meanTwo) * (attributesTwo[i] - meanTwo));
			}
			return Math.Max(0, 1 - (covariance / Math.Sqrt(standardDeviationOne * standardDeviationTwo)));
		}
	}
}
