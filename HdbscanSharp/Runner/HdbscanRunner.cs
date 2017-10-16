using HdbscanSharp.Distance;
using HdbscanSharp.Hdbscanstar;
using HdbscanSharp.Runner;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Runner
{
	public class HdbscanRunner
	{
		public static HdbscanResult Run(HdbscanParameters parameters)
		{
			int numPoints = parameters.DataSet.Length;
			
			double[] coreDistances = HdbscanAlgorithm.CalculateCoreDistances(
				parameters.DataSet,
				parameters.MinPoints,
				parameters.DistanceFunction);

			UndirectedGraph mst = HdbscanAlgorithm.ConstructMST(
				parameters.DataSet,
				coreDistances,
				true,
				parameters.DistanceFunction);
			mst.QuicksortByEdgeWeight();

			double[] pointNoiseLevels = new double[numPoints];
			int[] pointLastClusters = new int[numPoints];

			StringBuilder hierarchyWriter = new StringBuilder();
			char delimiter = ',';

			List<Cluster> clusters = HdbscanAlgorithm.ComputeHierarchyAndClusterTree(
				mst,
				parameters.MinClusterSize,
				true,
				parameters.Constraints,
				hierarchyWriter,
				delimiter,
				pointNoiseLevels,
				pointLastClusters);

			bool infiniteStability = HdbscanAlgorithm.PropagateTree(clusters);
			int[] prominentClusters = HdbscanAlgorithm.FindProminentClusters(
				clusters,
				hierarchyWriter,
				delimiter,
				numPoints);

			List<OutlierScore> scores = HdbscanAlgorithm.CalculateOutlierScores(
				clusters,
				pointNoiseLevels,
				pointLastClusters,
				coreDistances);

			return new HdbscanResult
			{
				Labels = prominentClusters,
				OutliersScore = scores,
				HasInfiniteStability = infiniteStability
			};
		}
	}
}
