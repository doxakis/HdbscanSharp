using HdbscanSharp.Distance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
	[TestClass]
	public class CosineSimilarityTests
    {
		[TestMethod]
		public void TestDistanceIsPositiveEvenIfThereIsRounding()
		{
			// See: https://github.com/doxakis/HdbscanSharp/issues/5

			var a = new double[] { 20 };
			var b = new double[] { 19.990000000000002 };

			var distFunc = new CosineSimilarity();
			var distance = distFunc.ComputeDistance(a, b);
			if (distance < 0)
			{
				Assert.Fail("Distance must be positive.");
			}
		}
	}
}
