using HdbscanSharp.Distance;
using System.Collections.Generic;
using HdbscanSharp.Hdbscanstar;

namespace HdbscanSharp.Runner
{
	public class HdbscanParameters
    {
        public bool UseMultipleThread { get; set; }
        public int MaxDegreeOfParallelism { get; set; }

        public double[][] Distances { get; set; }
		public double[][] DataSet { get; set; }
		public IDistanceCalculator DistanceFunction { get; set; }

        public int MinPoints { get; set; }
		public int MinClusterSize { get; set; }
		public List<HdbscanConstraint> Constraints { get; set; }
	}
}
