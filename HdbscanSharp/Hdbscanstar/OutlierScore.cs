using System;

namespace HdbscanSharp.Hdbscanstar
{
	/// <summary>
	/// Simple storage class that keeps the outlier score, core distance, and id (index) for a single point.
	/// OutlierScores are sorted in ascending order by outlier score, with core distances used to break
	/// outlier score ties, and ids used to break core distance ties.
	/// </summary>
	public class OutlierScore : IComparable<OutlierScore>
	{
		public double CoreDistance { get; set; }
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
			CoreDistance = coreDistance;
			Id = id;
		}

		public int CompareTo(OutlierScore other)
		{
			if (Score > other.Score)
				return 1;
			
			if (Score < other.Score)
				return -1;
			
			if (CoreDistance > other.CoreDistance)
				return 1;
			
			if (CoreDistance < other.CoreDistance)
				return -1;
			
			return Id - other.Id;
		}
	}

	public class OutlierScore<T>(double score, double coreDistance, T item)
	{
		public double CoreDistance { get; set; } = coreDistance;

		public double Score { get; set; } = score;

		public T Item { get; set; } = item;
	}
}
