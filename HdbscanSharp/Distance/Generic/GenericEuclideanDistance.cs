using System;
using System.Numerics;
using System.Numerics.Tensors;

namespace HdbscanSharp.Distance;

#if NET8_0_OR_GREATER
public class GenericEuclideanDistance
{
    public static Func<int, int, double> GetFunc<T>(T[][] dataset) where T : IRootFunctions<T> => (index1, index2) =>
        double.CreateTruncating(TensorPrimitives.Distance<T>(dataset[index1], dataset[index2]));
}
#endif