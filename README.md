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
- .NET Core 2.0
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
  DataSet = dataset,
  MinPoints = 25,
  MinClusterSize = 25,
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

**For more complete examples, please see the DocumentClusteringExample and IrisDatasetExample folders.**

# Improve speed

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

# Copyright and license
Code released under the MIT license.
