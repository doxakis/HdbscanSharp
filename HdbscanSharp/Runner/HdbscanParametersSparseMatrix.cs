using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using HdbscanSharp.Distance;

namespace HdbscanSharp.Runner;

public class HdbscanParametersSparseMatrix<T> :
    HdbscanParametersBase<Dictionary<int, T>>
    where T : INumberBase<T>
{
    public ISparseMatrixDistanceCalculator<T> DistanceFunction { get; set; }

    private Dictionary<int, double> SparseDistance { get; set; }

    internal override void PrecomputeDistances()
    {
        var sparseDistance = new Dictionary<int, double>();

        var mostCommonDistanceValueForSparseMatrix =
            DistanceFunction.GetMostCommonDistanceValueForSparseMatrix();

        if (MaxDegreeOfParallelism is 0 or > 1)
        {
            var size = NumPoints * NumPoints;

            var maxDegreeOfParallelism = MaxDegreeOfParallelism;
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

                    var i = index % NumPoints;
                    var j = index / NumPoints;
                    if (i >= j)
                        continue;

                    var distance =
                        DistanceFunction.ComputeDistance(i, j, DataSet[i], DataSet[j]);

                    if (Math.Abs(distance - mostCommonDistanceValueForSparseMatrix) > double.Epsilon)
                        distanceThread.Add(i * NumPoints + j, distance);
                }

                lock (sparseDistance)
                    foreach (var d in distanceThread)
                        sparseDistance.Add(d.Key, d.Value);
            });
        }
        else
        {
            for (var i = 0; i < NumPoints; i++)
            {
                for (var j = 0; j < i; j++)
                {
                    var distance =
                        DistanceFunction.ComputeDistance(i, j, DataSet[i], DataSet[j]);

                    if (Math.Abs(distance - mostCommonDistanceValueForSparseMatrix) > double.Epsilon)
                        sparseDistance.Add(i * NumPoints + j, distance);
                }
            }
        }

        SparseDistance = sparseDistance;
    }

    internal override Func<int, int, double> GetDistanceFunc() =>
        (a, b) =>
        {
            if (a < b)
                return SparseDistance.ContainsKey(a * NumPoints + b)
                    ? SparseDistance[a * NumPoints + b]
                    : 1;

            return SparseDistance.ContainsKey(b * NumPoints + a)
                ? SparseDistance[b * NumPoints + a]
                : 1;
        };
}