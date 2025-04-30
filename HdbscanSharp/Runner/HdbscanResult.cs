using HdbscanSharp.Hdbscanstar;
using System.Collections.Generic;

namespace HdbscanSharp.Runner
{
	public class HdbscanResult
	{
		public int[] Labels { get; set; }
		public List<OutlierScore> OutliersScore { get; set; }
		public bool HasInfiniteStability { get; set; }
	}

	public class HdbscanResult<T>
	{
		public Dictionary<int, List<T>> Groups { get; set; }
		public List<OutlierScore<T>> OutliersScore { get; set; }
		public bool HasInfiniteStability { get; set; }
	}
}
