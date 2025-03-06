using HdbscanSharp.Distance;
using HdbscanSharp.Runner;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests
{
    public class TestPearsonCorrelation
	{
        [Fact]
		public void TestDistanceIsPositiveEvenIfThereIsRounding()
		{
			// See: https://github.com/doxakis/HdbscanSharp/issues/5

			var a = new double[] { 21.33, 21.33, 21.33, 21.33, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			var b = new double[] { 19.99, 19.99, 19.99, 19.990000000000002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

			var distFunc = new PearsonCorrelation();
			var distance = distFunc.ComputeDistance(0, 1, a, b);
			if (distance < 0)
			{
				Assert.Fail("Distance must be positive.");
			}
		}
		
        [Fact]
        public void TestValidateOutlierScoreBetweenZeroAndOne()
        {
			// Cluster 1
			var a = new double[] { 21.33, 21.33, 21.33, 21.33, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			var b = new double[] { 19.99, 19.99, 19.99, 19.990000000000002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

			// Cluster 2
			var c = new double[] { 1, 2, 3, 4, 5, 6, 6, 7, 7, 9, 3, 4, 3, 2, 2, 1 };
			var d = new double[] { 1, 3, 3, 5, 5, 6, 6, 8, 8, 9, 3, 4, 3, 2, 1, 2 };

			// Outliers
			var e = new double[] { 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9 };
			var f = new double[] { 1, 2, 3, 0, 5, 6, 0, 0, 7, 9, 0, 0, 3, 0, 2, 0 };
			
			var dataset = new List<double[]>();
			dataset.Add(a);
			dataset.Add(b);
			dataset.Add(c);
			dataset.Add(d);
			dataset.Add(e);
			dataset.Add(f);
			
			var result = HdbscanRunner.Run(dataset.Count, 2, 2, DistanceHelpers.GetFunc(new PearsonCorrelation(), dataset.ToArray()));
			
			var numInvalidScore = result.OutliersScore.Count(m => m.Score < 0 || m.Score > 1);
			Assert.Equal(0, numInvalidScore);
		}
    }
}
