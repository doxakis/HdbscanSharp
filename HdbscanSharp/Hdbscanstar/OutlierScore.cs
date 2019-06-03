using System;

namespace HdbscanSharp.Hdbscanstar
{
	/// <summary>
	/// Simple storage class that keeps the outlier score, core distance, and id (index) for a single point.
	/// OutlierScores are sorted in ascending order by outlier score, with core distances used to break
	/// outlier score ties, and ids used to break core distance ties.
	/// </summary>
	public class OutlierScore : IComparable<OutlierScore> {

		private readonly double _coreDistance;

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
			Score = score;
			_coreDistance = coreDistance;
			Id = id;
		}

		public int CompareTo(OutlierScore other)
		{
			if (Score > other.Score)
				return 1;
			
			if (Score < other.Score)
				return -1;
			
			if (_coreDistance > other._coreDistance)
				return 1;
			
			if (_coreDistance < other._coreDistance)
				return -1;
			
			return Id - other.Id;
		}
	}
}
