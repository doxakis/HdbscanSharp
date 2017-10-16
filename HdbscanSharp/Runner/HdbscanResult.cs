using HdbscanSharp.Hdbscanstar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Runner
{
	public class HdbscanResult
	{
		public int[] Labels { get; set; }
		public List<OutlierScore> OutliersScore { get; set; }
		public bool HasInfiniteStability { get; set; }
	}
}
