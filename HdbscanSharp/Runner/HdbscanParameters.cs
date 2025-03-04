using System;
using HdbscanSharp.Distance;
using System.Numerics;
using System.Threading.Tasks;

namespace HdbscanSharp.Runner;

public class HdbscanParameters<T> : HdbscanParametersBase<T[]>
    where T : INumberBase<T>
{
    public IDistanceCalculator<T> DistanceFunction { get; set; }

    internal override void PrecomputeDistances()
    {
        if (this is not { Distances: null, CacheDistance: true })
            return;

        var distances = new double[NumPoints][];
        for (var i = 0; i < distances.Length; i++)
        {
            distances[i] = new double[NumPoints];
        }

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

            Parallel.For(0, size, option, index =>
            {
                var i = index % NumPoints;
                var j = index / NumPoints;
                if (i < j)
                {
                    var distance = DistanceFunction.ComputeDistance(DataSet[i], DataSet[j]);
                    distances[i][j] = distance;
                    distances[j][i] = distance;
                }
            });
        }
        else
        {
            for (var i = 0; i < NumPoints; i++)
            {
                for (var j = 0; j < i; j++)
                {
                    var distance = DistanceFunction.ComputeDistance(DataSet[i], DataSet[j]);
                    distances[i][j] = distance;
                    distances[j][i] = distance;
                }
            }
        }

        Distances = distances;
    }

    internal override Func<int, int, double> GetDistanceFunc()
    {
        // Normal matrix with caching.
        if (CacheDistance)
            return (a, b) => Distances[a][b];

        // No cache
        return (a, b) =>
            DistanceFunction.ComputeDistance(DataSet[a], DataSet[b]);
    }
}