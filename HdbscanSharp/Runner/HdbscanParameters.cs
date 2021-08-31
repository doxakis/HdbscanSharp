using HdbscanSharp.Distance;
using System.Collections.Generic;
using HdbscanSharp.Hdbscanstar;

namespace HdbscanSharp.Runner
{
	public class HdbscanParameters<T>
    {
        public bool CacheDistance { get; set; } = true;
        public int MaxDegreeOfParallelism { get; set; } = 1;

        public double[][] Distances { get; set; }
		public T[] DataSet { get; set; }
		public IDistanceCalculator<T> DistanceFunction { get; set; }

        public int MinPoints { get; set; }
		public int MinClusterSize { get; set; }
		public List<HdbscanConstraint> Constraints { get; set; }
	}
}
