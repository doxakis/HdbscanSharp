# HdbscanSharp
HDBSCAN is a clustering algorithm developed by Campello, Moulavi, and Sander. It extends DBSCAN by converting it into a hierarchical clustering algorithm, and then using a technique to extract a flat clustering based in the stability of clusters.

> It performs DBSCAN over varying epsilon values and integrates the result to find a clustering that gives the best stability over epsilon. This allows HDBSCAN to find clusters of varying densities (unlike DBSCAN), and be more robust to parameter selection.
> 
> In practice this means that HDBSCAN returns a good clustering straight away with little or no parameter tuning and the primary parameter, minimum cluster size, is intuitive and easy to select.
> 
> HDBSCAN is ideal for exploratory data analysis; it's a fast and robust algorithm that you can trust to return meaningful clusters (if there are any).
> 
> From: https://github.com/scikit-learn-contrib/hdbscan

# Supported framework
- .NET Standard 2.0
- .NET 8.0 or latest

# Install from Nuget
To get the latest version:
```
Install-Package HdbscanSharp
```

# Examples

## Grouping coordinates

```csharp
using HdbscanSharp.Distance;
using HdbscanSharp.Runner;

List<(int X, int Y)> points =
[
    (0, 0), (0, 1), (1, 0), (2, 3), // A group near coordinate (0, 0)
    (100, 97), (98, 100), (98, 95), (97, 94), // A group near coordinate (100, 100)
    (-100, 97), (-98, 100), (-98, 95), (-97, 94), (-90, 93), // A group near coordinate (-100, 100)
    (-500, 1000), (500, 1000) // Two outliers
];

var result = HdbscanRunner.Run(points, point => new float[] {point.X, point.Y}, 3, 3, GenericEuclideanDistance.GetFunc);

foreach (var group in result.Groups)
    Console.WriteLine("Group: " + string.Join(" ", group.Value.Select(x => "(" + x.X + ", " + x.Y + ")")));

// The output would be:
// Group: (-500, 1000) (500, 1000)
// Group: (100, 97) (98, 100) (98, 95) (97, 94)
// Group: (0, 0) (0, 1) (1, 0) (2, 3)
// Group: (-100, 97) (-98, 100) (-98, 95) (-97, 94) (-90, 93)
```

## A more complete example

```csharp
using HdbscanSharp.Distance;
using HdbscanSharp.Hdbscanstar;
using HdbscanSharp.Runner;

// (you need to have at least .net 8. Otherwise, you are limited to use DistanceHelpers since GenericCosineSimilarity use TensorPrimitives for generic math)
// (By using TensorPrimitives, it can use hardware acceleration (Advanced Vector Extensions, aka AVX) when available)

var result = HdbscanRunner.Run(
    dataset.Length, // The number of element in the dataset
    25, // MinPoints
    25, // MinClusterSize
    GenericCosineSimilarity.GetFunc(dataset)); // dataset is float[][] or double[][]. You may also use GenericEuclideanDistance for euclidean distance.

// Or with DistanceHelpers: (more distances available and different options like sparse matrix, caching and multi-threading)

var result = HdbscanRunner.Run(
    dataset.Length, // The number of element in the dataset
    25, // MinPoints
    25, // MinClusterSize
    DistanceHelpers.GetFunc(new CosineSimilarity( // See HdbscanSharp/Distance/NonGeneric folder for more distance function
        false, // use caching for distance
        false), // specify if used with multiple threads
    dataset, // double[][] for normal matrix or Dictionary<int, int>[] for sparse matrix
    null,
    true, // use caching for distance
    1)); // to indicate to use all threads, you can specify 0.

// ...

// result.Labels : Prominent Clusters
// It is an array with an integer for each data sample.
// Samples that are in the same cluster get assigned the same number.

// result.OutliersScore : for each data sample. (Id = Index in the array)
// Outliers Score are sorted in ascending order by outlier score, with core distances used to break
// outlier score ties, and ids used to break core distance ties.

// if result.HasInfiniteStability is true:
// With your current settings, the K-NN density estimate is discontinuous as it is not well-defined
// (infinite) for some data objects, either due to replicates in the data (not a set) or due to numerical
// roundings. This does not affect the construction of the density-based clustering hierarchy, but
// it affects the computation of cluster stability by means of relative excess of mass. For this reason,
// the post-processing routine to extract a flat partition containing the most stable clusters may
// produce unexpected results. It may be advisable to increase the value of MinPoints and/or MinClusterSize.
```

**For more complete example, please see the project IrisDatasetExample.**

# Improving performance

You have many options.

## Use hardware acceleration (Advanced Vector Extensions, aka AVX) when available

By using GenericCosineSimilarity/GenericEuclideanDistance or your own version of the distance function. Consider using TensorPrimitives.

## Reduce the dataset

Split the dataset in multiple smaller batches and run the algorithm for each batch.
The algorithm does not scale linearly.
At least, it can return clusters while using less memory, CPU and time.

## Use caching (with maybe multiple threads)

2 ways:

- The `CosineSimilarity` class accept a parameter to indicate if you want caching enable and a second parameter to indicate if you will use it with multiple threads.
- Use the parameter `CacheDistance`

Note:
If CacheDistance is false, MaxDegreeOfParallelism has no impact.
The CacheDistance can be useful to use multiple threads to calculate the distance matrix.
But, it can use a lot a memory depending on the DataSet and unfortunately, it may impact negatively the performance if too much memory is used. So, use with caution and measure.

## Use sparse matrix

If the dataset is filled with mostly the same values, you could use sparse matrix.
The algorithm will skip the missing values and it could massively improve the performance by having less calculation to do.

## Reduce the dimensions

In order to speed up the overall process if you have a lot of vectors with high dimensions, I suggest to do a Principal Component Analysis.
The Accord.NET Framework provide a great implementation. (http://accord-framework.net/)

You can train on a random subset to determine the transform matrix.

```
using Accord.Statistics.Analysis;

...

PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis();
pca.NumberOfOutputs = 3;
var trainingVectors = vectors.ToArray();
Shuffle(trainingVectors);
trainingVectors = trainingVectors.Take(300).ToArray();
var pcaResult = pca.Learn(trainingVectors);
var reducedVectorsWithPCA = pcaResult.Transform(vectors.ToArray());
```

## Precompute the distance between each element

You can provide the distance matrix. Let's consider you have N elements in the dataset. The distance matrix would be N x N elements. You may consider to cache the result.

## GPU acceleration

Note: I assume you have a Nvidia graphic card.

I suggest to use ILGPU (http://www.ilgpu.net/) (dotnet core support)

Be aware that there is a cost time to communicate with the GPU. So you need lot of data to benefits. The memory can be limited depending on the use case and the GPU used. You may need to split your dataset, do batch process and combine the result.

First, you would write the kernel function and implement the distance function. (see folder: HdbscanSharp/Distance/NonGeneric)

Steps:

- Prepare the dataset in the right format
- Use the CUDA accelerator
- Create a Context with the accelerator
- Load kernel function (JIT compilation take about 1 sec and occur only on the first call)
- Allocate memory on GPU (dataset and distance matrix) and keep a reference.
- Copy dataset on GPU memory
- Call the kernel function
- On the accelerator context call `Synchronize()`
- Get the distance matrix with `GetAsArray()` on the reference you use previously to assign memory on GPU
- Prepare the matrix in the right format
- You would provide the distances matrix to Hdbscan

# Copyright and license
Code released under the MIT license.
