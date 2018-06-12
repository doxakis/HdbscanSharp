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

            if (parameters.Distances == null)
            {
                // Precompute distances.
                double[][] distances = new double[numPoints][];
                for (int i = 0; i < distances.Length; i++)
                {
                    distances[i] = new double[numPoints];
                }

                for (int i = 0; i < numPoints; i++)
                {
                    for (int j = 0; j < numPoints; j++)
                    {
                        double distance = parameters.DistanceFunction.ComputeDistance(
                            parameters.DataSet[i],
                            parameters.DataSet[j]);
                        distances[i][j] = distance;
                        distances[j][i] = distance;
                    }
                }
                parameters.Distances = distances;
            }
            
			// Compute core distances
			double[] coreDistances = HdbscanAlgorithm.CalculateCoreDistances(
				parameters.Distances,
				parameters.MinPoints);

			// Calculate minimum spanning tree
			UndirectedGraph mst = HdbscanAlgorithm.ConstructMST(
				parameters.Distances,
				coreDistances,
				true);
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
