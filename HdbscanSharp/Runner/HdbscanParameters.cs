using HdbscanSharp.Distance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HdbscanSharp.Hdbscanstar;

namespace HdbscanSharp.Runner
{
	public class HdbscanParameters
    {
		public double[][] DataSet { get; set; }
		public int MinPoints { get; set; }
		public int MinClusterSize { get; set; }
		public IDistanceCalculator DistanceFunction { get; set; }
		public List<HdbscanConstraint> Constraints { get; set; }
	}
}
