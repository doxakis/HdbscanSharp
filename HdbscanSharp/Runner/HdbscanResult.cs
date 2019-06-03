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
}
