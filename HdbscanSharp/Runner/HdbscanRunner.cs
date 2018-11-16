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
			var numPoints = parameters.DataSet != null
                ? parameters.DataSet.Length
                : parameters.Distances.Length;

            if (parameters.Distances == null)
            {
                // Precompute distances.
                var distances = new double[numPoints][];
                for (var i = 0; i < distances.Length; i++)
                {
                    distances[i] = new double[numPoints];
                }

                if (parameters.UseMultipleThread)
                {
                    var size = numPoints * numPoints;
                    
                    var maxDegreeOfParallelism = parameters.MaxDegreeOfParallelism;
                    if (maxDegreeOfParallelism == 0)
                    {
                        // Not specified. Use all threads.
                        maxDegreeOfParallelism = Environment.ProcessorCount;
                    }
                    var option = new ParallelOptions {
                        MaxDegreeOfParallelism = Math.Max(1, maxDegreeOfParallelism)
                    };
                    
                    Parallel.For(0, size, option, index =>
                    {
                        var i = index % numPoints;
                        var j = index / numPoints;
                        if (i < j)
                        {
                            var distance = parameters.DistanceFunction.ComputeDistance(
                                    parameters.DataSet[i],
                                    parameters.DataSet[j]);
                            distances[i][j] = distance;
                            distances[j][i] = distance;
                        }
                    });
                }
                else
                {
                    for (var i = 0; i < numPoints; i++)
                    {
                        for (var j = 0; j < i; j++)
                        {
                            var distance = parameters.DistanceFunction.ComputeDistance(
                                parameters.DataSet[i],
                                parameters.DataSet[j]);
                            distances[i][j] = distance;
                            distances[j][i] = distance;
                        }
                    }
                }

                parameters.Distances = distances;
            }
            
			// Compute core distances
			var coreDistances = HdbscanAlgorithm.CalculateCoreDistances(
				parameters.Distances,
				parameters.MinPoints);

			// Calculate minimum spanning tree
			var mst = HdbscanAlgorithm.ConstructMst(
				parameters.Distances,
				coreDistances,
				true);
			mst.QuicksortByEdgeWeight();

			var pointNoiseLevels = new double[numPoints];
			var pointLastClusters = new int[numPoints];

			var hierarchyWriter = new StringBuilder();
			var delimiter = ',';

			// Compute hierarchy and cluster tree
			var clusters = HdbscanAlgorithm.ComputeHierarchyAndClusterTree(
				mst,
				parameters.MinClusterSize,
				true,
				parameters.Constraints,
				hierarchyWriter,
				delimiter,
				pointNoiseLevels,
				pointLastClusters);

			// Propagate clusters
			var infiniteStability = HdbscanAlgorithm.PropagateTree(clusters);

			// Compute final flat partitioning
			var prominentClusters = HdbscanAlgorithm.FindProminentClusters(
				clusters,
				hierarchyWriter,
				delimiter,
				numPoints);

			// Compute outlier scores for each point
			var scores = HdbscanAlgorithm.CalculateOutlierScores(
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
