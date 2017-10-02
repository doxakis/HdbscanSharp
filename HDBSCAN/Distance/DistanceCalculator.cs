using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDBSCAN.Distance
{
	/**
	 * An interface for classes which compute the distance between two points (where points are
	 * represented as arrays of doubles).
	 */
	public interface IDistanceCalculator
	{
		/**
		 * Computes the distance between two points.  Note that larger values indicate that the two points
		 * are farther apart.
		 * @param attributesOne The attributes of the first point
		 * @param attributesTwo The attributes of the second point
		 * @return A double for the distance between the two points
		 */
		double ComputeDistance(double[] attributesOne, double[] attributesTwo);
		string GetName();
	}
}
