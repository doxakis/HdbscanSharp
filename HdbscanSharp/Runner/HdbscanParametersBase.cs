using System;
using System.Collections.Generic;
using HdbscanSharp.Hdbscanstar;

namespace HdbscanSharp.Runner;

public abstract class HdbscanParametersBase<T>
{
    public bool CacheDistance { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = 1;

    public T[] DataSet { get; set; }
    public double[][] Distances { get; set; }

    public int MinPoints { get; set; }
    public int MinClusterSize { get; set; }
    public List<HdbscanConstraint> Constraints { get; set; }

    internal int NumPoints => DataSet?.Length ?? Distances.Length;

    internal abstract void PrecomputeDistances();
    internal abstract Func<int, int, double> GetDistanceFunc();
}