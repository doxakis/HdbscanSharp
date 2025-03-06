using HdbscanSharp.Hdbscanstar;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HdbscanSharp.Distance;

namespace HdbscanSharp.Runner
{
    public class HdbscanRunner
    {
        public static HdbscanResult Run(
            int datasetCount,
            int minPoints,
            int minClusterSize,
            Func<int, int, double> distanceFunc,
            List<HdbscanConstraint> constraints = null)
        {
            var numPoints = datasetCount;

            // Compute core distances
            var coreDistances = HdbscanAlgorithm.CalculateCoreDistances(
                distanceFunc,
                numPoints,
                minPoints);

            // Calculate minimum spanning tree
            var mst = HdbscanAlgorithm.ConstructMst(
                distanceFunc,
                numPoints,
                coreDistances,
                true);
            mst.QuicksortByEdgeWeight();

            var pointNoiseLevels = new double[numPoints];
            var pointLastClusters = new int[numPoints];
            var hierarchy = new List<int[]>();

            // Compute hierarchy and cluster tree
            var clusters = HdbscanAlgorithm.ComputeHierarchyAndClusterTree(
                mst,
                minClusterSize,
                constraints,
                hierarchy,
                pointNoiseLevels,
                pointLastClusters);

            // Propagate clusters
            var infiniteStability = HdbscanAlgorithm.PropagateTree(clusters);

            // Compute final flat partitioning
            var prominentClusters = HdbscanAlgorithm.FindProminentClusters(
                clusters,
                hierarchy,
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