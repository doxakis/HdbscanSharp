﻿using System;

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
			for (var i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
			{
				var difference = Math.Abs(attributesOne[i] - attributesTwo[i]);
				if (difference > distance)
					distance = difference;
			}
			return distance;
		}
	}
}
