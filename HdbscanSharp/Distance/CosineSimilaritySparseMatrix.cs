using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HdbscanSharp.Distance;

/// <summary>
/// Computes cosine distance between two points, d = 1 - ((X*Y) / (||X||*||Y||))
/// </summary>
public class CosineSimilaritySparseMatrix<T> : ISparseMatrixDistanceCalculator<T>
    where T : IRootFunctions<T>, INumber<T>, IDivisionOperators<T, T, T>
{
    private readonly Func<int, (bool, T)> _tryGet;
    private readonly Action<int, T> _tryAdd;

    public CosineSimilaritySparseMatrix(bool useCaching = false, bool usedWithMultipleThreads = false)
    {
        if (!useCaching)
        {
            // No caching. Do nothing.
            _tryGet = _ => (false, T.AdditiveIdentity);
            _tryAdd = (_, _) => { };
        }
        else
        {
            if (usedWithMultipleThreads)
            {
                var cache = new ConcurrentDictionary<int, T>();

                _tryGet = index =>
                {
                    var hasValue = cache.TryGetValue(index, out var value);
                    return (hasValue, value);
                };
                _tryAdd = (index, value) => cache.TryAdd(index, value);
            }
            else
            {
                var cache = new Dictionary<int, T>();

                _tryGet = index =>
                {
                    var hasValue = cache.TryGetValue(index, out var value);
                    return (hasValue, value);
                };
                _tryAdd = (index, value) => cache.TryAdd(index, value);
            }
        }
    }

    public double GetMostCommonDistanceValueForSparseMatrix() => 1;

    public double ComputeDistance(
        int indexOne,
        int indexTwo,
        Dictionary<int, T> attributesOne,
        Dictionary<int, T> attributesTwo)
    {
        var magnitudeOne = CalculateAndCacheMagnitude(indexOne, attributesOne);
        var magnitudeTwo = CalculateAndCacheMagnitude(indexTwo, attributesTwo);

        if (magnitudeOne == T.AdditiveIdentity && magnitudeTwo == T.AdditiveIdentity)
            return 1;

        var dotProduct = T.AdditiveIdentity;
        if (attributesOne.Count < attributesTwo.Count)
        {
            foreach (var i in attributesOne.Keys)
                if (attributesTwo.TryGetValue(i, out var value))
                    dotProduct += attributesOne[i] * value;
        }
        else
        {
            foreach (var i in attributesTwo.Keys)
                if (attributesOne.TryGetValue(i, out var value))
                    dotProduct += value * attributesTwo[i];
        }

        var result = T.Max(
            T.AdditiveIdentity,
            T.MultiplicativeIdentity - dotProduct / T.Sqrt(magnitudeOne * magnitudeTwo));
        return double.CreateTruncating(result);
    }

    private T CalculateAndCacheMagnitude(int index, Dictionary<int, T> attributes)
    {
        var (hasValue, value) = _tryGet(index);
        if (hasValue)
            return value;

        var magnitude = attributes.Keys.Select(i => attributes[i] * attributes[i])
            .Aggregate(T.AdditiveIdentity, (a, b) => a + b);
        _tryAdd(index, magnitude);
        return magnitude;
    }
}