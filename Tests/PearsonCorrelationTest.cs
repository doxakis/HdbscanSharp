using HdbscanSharp.Distance;
using HdbscanSharp.Runner;
using System.Linq;
using Xunit;

namespace Tests;

public class TestPearsonCorrelation
{
	[Fact]
	public void TestDistanceIsPositiveEvenIfThereIsRounding()
	{
		// See: https://github.com/doxakis/HdbscanSharp/issues/5

		double[] a = [21.33, 21.33, 21.33, 21.33, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		double[] b = [19.99, 19.99, 19.99, 19.990000000000002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

		var distFunc = new PearsonCorrelation<double>();
		var distance = distFunc.ComputeDistance(a, b);
		if (distance < 0)
		{
			Assert.Fail("Distance must be positive.");
		}
	}
		
	[Fact]
	public void TestValidateOutlierScoreBetweenZeroAndOne()
	{
		// Cluster 1
		double[] a = [21.33, 21.33, 21.33, 21.33, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		double[] b = [19.99, 19.99, 19.99, 19.990000000000002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

		// Cluster 2
		double[] c = [1, 2, 3, 4, 5, 6, 6, 7, 7, 9, 3, 4, 3, 2, 2, 1];
		double[] d = [1, 3, 3, 5, 5, 6, 6, 8, 8, 9, 3, 4, 3, 2, 1, 2];

		// Outliers
		double[] e = [9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9];
		double[] f = [1, 2, 3, 0, 5, 6, 0, 0, 7, 9, 0, 0, 3, 0, 2, 0];

		double[][] dataset =
		[
			a,
			b,
			c,
			d,
			e,
			f
		];

		var result = HdbscanRunner.Run(new HdbscanParameters<double>
		{
			DataSet = dataset.ToArray(),
			MinPoints = 2,
			MinClusterSize = 2,
			DistanceFunction = new PearsonCorrelation<double>()
		});
			
		var numInvalidScore = result.OutliersScore.Count(m => m.Score is < 0 or > 1);
		Assert.Equal(0, numInvalidScore);
	}
}