using HdbscanSharp.Hdbscanstar;
using System.Collections.Generic;

namespace HdbscanSharp.Runner;

public class HdbscanRunner
{
    public static HdbscanResult Run<T>(HdbscanParametersBase<T> parameters)
    {
        var numPoints = parameters.NumPoints;
            
        parameters.PrecomputeDistances();
        var internalDistanceFunc = parameters.GetDistanceFunc();

        // Compute core distances
        var coreDistances = HdbscanAlgorithm.CalculateCoreDistances(
            internalDistanceFunc,
            numPoints,
            parameters.MinPoints);

        // Calculate minimum spanning tree
        var mst = HdbscanAlgorithm.ConstructMst(
            internalDistanceFunc,
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
            parameters.MinClusterSize,
            parameters.Constraints,
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