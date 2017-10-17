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

			// Compute core distances
			double[] coreDistances = HdbscanAlgorithm.CalculateCoreDistances(
				parameters.DataSet,
				parameters.MinPoints,
				parameters.DistanceFunction);

			// Calculate minimum spanning tree
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

			// Compute hierarchy and cluster tree
			List<Cluster> clusters = HdbscanAlgorithm.ComputeHierarchyAndClusterTree(
				mst,
				parameters.MinClusterSize,
				true,
				parameters.Constraints,
				hierarchyWriter,
				delimiter,
				pointNoiseLevels,
				pointLastClusters);

			// Propagate clusters
			bool infiniteStability = HdbscanAlgorithm.PropagateTree(clusters);

			// Compute final flat partitioning
			int[] prominentClusters = HdbscanAlgorithm.FindProminentClusters(
				clusters,
				hierarchyWriter,
				delimiter,
				numPoints);

			// Compute outlier scores for each point
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
