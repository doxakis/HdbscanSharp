using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HdbscanSharp.Distance;

public class DistanceHelpers
{
    public static Func<int, int, double> GetFunc<T>(
        IDistanceCalculator<T> distanceFunction,
        T[] dataSet = null,
        double[][] distances = null,
        bool cacheDistance = true,
        int maxDegreeOfParallelism = 1)
    {
        var numPoints = dataSet?.Length ?? distances.Length;
        distances ??= PrecomputeNormalMatrixDistancesIfApplicable(dataSet, distanceFunction, cacheDistance, maxDegreeOfParallelism, numPoints);
        var sparseDistance = PrecomputeSparseMatrixDistancesIfApplicable(distances, dataSet, distanceFunction, cacheDistance, maxDegreeOfParallelism, numPoints);
        return DetermineInternalDistanceFunc(distances, dataSet, distanceFunction, cacheDistance, sparseDistance, numPoints);
    }
    
    private static Func<int, int, double> DetermineInternalDistanceFunc<T>(
         double[][] distances,
         T[] dataSet,
         IDistanceCalculator<T> distanceFunction,
         bool cacheDistance,
         IReadOnlyDictionary<int, double> sparseDistance,
         int numPoints)
     {
         // Sparse matrix with caching.
         if (sparseDistance != null)
             return (a, b) =>
             {
                 if (a < b)
                     return sparseDistance.ContainsKey(a * numPoints + b) ? sparseDistance[a * numPoints + b] : 1;
    
                 return sparseDistance.ContainsKey(b * numPoints + a) ? sparseDistance[b * numPoints + a] : 1;
             };
    
         // Normal matrix with caching.
         if (cacheDistance)
             return (a, b) => distances[a][b];
    
         // No cache
         return (a, b) =>
             distanceFunction.ComputeDistance(a, b, dataSet[a], dataSet[b]);
     }
    
     private static Dictionary<int, double> PrecomputeSparseMatrixDistancesIfApplicable<T>(
         double[][] distances,
         T[] dataSet,
         IDistanceCalculator<T> distanceFunction,
         bool cacheDistance,
         int maxDegreeOfParallelism,
         int numPoints)
     {
         Dictionary<int, double> sparseDistance = null;
         if (distances == null && cacheDistance &&
             dataSet is Dictionary<int, int>[])
         {
             sparseDistance = new Dictionary<int, double>();
             if (distanceFunction is not ISparseMatrixSupport sparseMatrixSupport)
                 throw new NotSupportedException("The distance function used does not support sparse matrix.");
             
             var mostCommonDistanceValueForSparseMatrix = sparseMatrixSupport.GetMostCommonDistanceValueForSparseMatrix();
    
             if (maxDegreeOfParallelism is 0 or > 1)
             {
                 var size = numPoints * numPoints;
    
                 if (maxDegreeOfParallelism == 0)
                 {
                     // Not specified. Use all threads.
                     maxDegreeOfParallelism = Environment.ProcessorCount;
                 }
    
                 var option = new ParallelOptions
                 {
                     MaxDegreeOfParallelism = Math.Max(1, maxDegreeOfParallelism)
                 };
    
                 Parallel.For(0, option.MaxDegreeOfParallelism, option, indexThread =>
                 {
                     var distanceThread = new Dictionary<int, double>();
    
                     for (var index = 0; index < size; index++)
                     {
                         if (index % option.MaxDegreeOfParallelism != indexThread)
                             continue;
    
                         var i = index % numPoints;
                         var j = index / numPoints;
                         if (i >= j)
                             continue;
    
                         var distance = distanceFunction.ComputeDistance(
                             i,
                             j,
                             dataSet[i],
                             dataSet[j]);
    
                         // ReSharper disable once CompareOfFloatsByEqualityOperator
                         if (distance != mostCommonDistanceValueForSparseMatrix)
                             distanceThread.Add(i * numPoints + j, distance);
                     }
    
                     lock (sparseDistance)
                         foreach (var d in distanceThread)
                             sparseDistance.Add(d.Key, d.Value);
                 });
             }
             else
             {
                 for (var i = 0; i < numPoints; i++)
                 {
                     for (var j = 0; j < i; j++)
                     {
                         var distance = distanceFunction.ComputeDistance(
                             i,
                             j,
                             dataSet[i],
                             dataSet[j]);
    
                         // ReSharper disable once CompareOfFloatsByEqualityOperator
                         if (distance != mostCommonDistanceValueForSparseMatrix)
                             sparseDistance.Add(i * numPoints + j, distance);
                     }
                 }
             }
         }
    
         return sparseDistance;
     }
    
     private static double[][] PrecomputeNormalMatrixDistancesIfApplicable<T>(
         T[] dataSet,
         IDistanceCalculator<T> distanceFunction,
         bool cacheDistance,
         int maxDegreeOfParallelism,
         int numPoints)
     {
         if (cacheDistance && dataSet is double[][])
         {
             var distances = new double[numPoints][];
             for (var i = 0; i < distances.Length; i++)
             {
                 distances[i] = new double[numPoints];
             }
    
             if (maxDegreeOfParallelism is 0 or > 1)
             {
                 var size = numPoints * numPoints;
    
                 if (maxDegreeOfParallelism == 0)
                 {
                     // Not specified. Use all threads.
                     maxDegreeOfParallelism = Environment.ProcessorCount;
                 }
    
                 var option = new ParallelOptions
                 {
                     MaxDegreeOfParallelism = Math.Max(1, maxDegreeOfParallelism)
                 };
    
                 Parallel.For(0, size, option, index =>
                 {
                     var i = index % numPoints;
                     var j = index / numPoints;
                     if (i < j)
                     {
                         var distance = distanceFunction.ComputeDistance(
                             i,
                             j,
                             dataSet[i],
                             dataSet[j]);
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
                         var distance = distanceFunction.ComputeDistance(
                             i,
                             j,
                             dataSet[i],
                             dataSet[j]);
                         distances[i][j] = distance;
                         distances[j][i] = distance;
                     }
                 }
             }
    
             return distances;
         }

         return null;
     }
}