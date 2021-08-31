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
- .NET Framework 4.5

# Install from Nuget
To get the latest version:
```
Install-Package HdbscanSharp
```

# Examples
```
using HdbscanSharp.Distance;
using HdbscanSharp.Hdbscanstar;
using HdbscanSharp.Runner;

// ...

var result = HdbscanRunner.Run(new HdbscanParameters
{
  DataSet = dataset, // double[][] for normal matrix or Dictionary<int, int>[] for sparse matrix
  MinPoints = 25,
  MinClusterSize = 25,
  CacheDistance = true, // use caching for distance
  MaxDegreeOfParallelism = 1, // to indicate all threads, you can specify 0.
  DistanceFunction = new CosineSimilarity() // See HdbscanSharp/Distance folder for more distance function
});

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

```
double[][] distances;

// compute distances

var result = HdbscanRunner.Run(new HdbscanParameters
{
  Distances = distances,
  MinPoints = 25,
  MinClusterSize = 25,
  DistanceFunction = new CosineSimilarity() // See HdbscanSharp/Distance folder for more distance function
});
```

## GPU acceleration

Note: I assume you have a Nvidia graphic card.

I suggest to use ILGPU (http://www.ilgpu.net/) (dotnet core support)

Be aware that there is a cost time to communicate with the GPU. So you need lot of data to benefits. The memory can be limited depending on the use case and the GPU used. You may need to split your dataset, do batch process and combine the result.

First, you would write the kernel function and implement the distance function. (see folder: HdbscanSharp/Distance)

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
- You would provide the distances matrix to Hdbscan (option: Distances)

# Copyright and license
Code released under the MIT license.
