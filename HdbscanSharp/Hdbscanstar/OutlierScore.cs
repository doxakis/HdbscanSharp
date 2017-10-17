using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Hdbscanstar
{
	/// <summary>
	/// Simple storage class that keeps the outlier score, core distance, and id (index) for a single point.
	/// OutlierScores are sorted in ascending order by outlier score, with core distances used to break
	/// outlier score ties, and ids used to break core distance ties.
	/// </summary>
	public class OutlierScore : IComparable<OutlierScore> {

		private double CoreDistance;

		public double Score { get; set; }
		public int Id { get; set; }

		/// <summary>
		/// Creates a new OutlierScore for a given point.
		/// </summary>
		/// <param name="score">The outlier score of the point</param>
		/// <param name="coreDistance">The point's core distance</param>
		/// <param name="id">The id (index) of the point</param>
		public OutlierScore(double score, double coreDistance, int id)
		{
			this.Score = score;
			this.CoreDistance = coreDistance;
			this.Id = id;
		}

		public int CompareTo(OutlierScore other)
		{
			if (this.Score > other.Score)
				return 1;
			else if (this.Score < other.Score)
				return -1;
			else
			{
				if (this.CoreDistance > other.CoreDistance)
					return 1;
				else if (this.CoreDistance < other.CoreDistance)
					return -1;
				else
					return this.Id - other.Id;
			}
		}
	}
}
