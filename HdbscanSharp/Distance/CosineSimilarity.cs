using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HdbscanSharp.Distance
{
    /// <summary>
    /// Computes cosine similarity between two points, d = 1 - ((X*Y) / (||X||*||Y||))
    /// </summary>
    public class CosineSimilarity : IDistanceCalculator<double[]>, IDistanceCalculator<Dictionary<int, int>>, ISparseMatrixSupport
    {
        private readonly Func<int, (bool, double)> _tryGet;
        private readonly Action<int, double> _tryAdd;

        public CosineSimilarity(bool useCaching = false, bool usedWithMultipleThreads = false)
        {
            if (!useCaching)
            {
                // No caching. Do nothing.
                _tryGet = _ => (false, 0);
                _tryAdd = (_, _) => { };
            }
            else
            {
                if (usedWithMultipleThreads)
                {
                    var cache = new ConcurrentDictionary<int, double>();

                    _tryGet = index =>
                    {
                        var hasValue = cache.TryGetValue(index, out var value);
                        return (hasValue, value);
                    };
                    _tryAdd = (index, value) => cache.TryAdd(index, value);
                }
                else
                {
                    var cache = new Dictionary<int, double>();

                    _tryGet = index =>
                    {
                        var hasValue = cache.TryGetValue(index, out var value);
                        return (hasValue, value);
                    };
                    _tryAdd = (index, value) =>
                    {
                        if (!cache.ContainsKey(index))
                            cache.Add(index, value);
                    };
                }
            }
        }
        
        public double GetMostCommonDistanceValueForSparseMatrix() => 1;
        
        public double ComputeDistance(int indexOne, int indexTwo, double[] attributesOne, double[] attributesTwo)
        {
            var magnitudeOne = CalculateAndCacheMagnitude(indexOne, attributesOne);
            var magnitudeTwo = CalculateAndCacheMagnitude(indexTwo, attributesTwo);

            if (magnitudeOne == 0 && magnitudeTwo == 0)
                return 1;

            double dotProduct = 0;
            for (var i = 0; i < attributesOne.Length && i < attributesTwo.Length; i++)
                dotProduct += attributesOne[i] * attributesTwo[i];

            var computeDistance = Math.Max(0, 1 - dotProduct / Math.Sqrt(magnitudeOne * magnitudeTwo));
            return computeDistance;
        }

        public double ComputeDistance(int indexOne, int indexTwo, Dictionary<int, int> attributesOne,
            Dictionary<int, int> attributesTwo)
        {
            var magnitudeOne = CalculateAndCacheMagnitude(indexOne, attributesOne);
            var magnitudeTwo = CalculateAndCacheMagnitude(indexTwo, attributesTwo);

            if (magnitudeOne == 0 && magnitudeTwo == 0)
                return 1;

            double dotProduct = 0;
            if (attributesOne.Count < attributesTwo.Count)
            {
                foreach (var i in attributesOne.Keys)
                    if (attributesTwo.ContainsKey(i))
                        dotProduct += attributesOne[i] * attributesTwo[i];
            }
            else
            {
                foreach (var i in attributesTwo.Keys)
                    if (attributesOne.ContainsKey(i))
                        dotProduct += attributesOne[i] * attributesTwo[i];
            }

            return Math.Max(0, 1 - dotProduct / Math.Sqrt(magnitudeOne * magnitudeTwo));
        }

        private double CalculateAndCacheMagnitude(int index, Dictionary<int, int> attributes)
        {
            var (hasValue, value) = _tryGet(index);
            if (hasValue)
                return value;

            var magnitude = attributes.Keys.Sum(i => Math.Pow(attributes[i], 2));
            _tryAdd(index, magnitude);
            return magnitude;
        }

        private double CalculateAndCacheMagnitude(int index, double[] attributes)
        {
            var (hasValue, value) = _tryGet(index);
            if (hasValue)
                return value;

            var magnitude = attributes.Sum(val => Math.Pow(val, 2));
            _tryAdd(index, magnitude);
            return magnitude;
        }
    }
}