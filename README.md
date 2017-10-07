# HdbscanSharp

HDBSCAN implementation in C#.

# Description
HDBSCAN is a clustering algorithm developed by Campello, Moulavi, and Sander.

> It performs DBSCAN over varying epsilon values and integrates the result to find a clustering that gives the best stability over epsilon. This allows HDBSCAN to find clusters of varying densities (unlike DBSCAN), and be more robust to parameter selection.
>
> In practice this means that HDBSCAN returns a good clustering straight away with little or no parameter tuning and the primary parameter, minimum cluster size, is intuitive and easy to select.
>
> HDBSCAN is ideal for exploratory data analysis; it's a fast and robust algorithm that you can trust to return meaningful clusters (if there are any).
>
> From: https://github.com/scikit-learn-contrib/hdbscan

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

# TODO List

As of now, the HDBSCAN algorithm works. But, it needs some works to make it awesome.

- [ ] Respect C# convention
- [ ] Remove intermediate generated files
- [ ] Expose an easy way to specify which parameters to use
- [ ] Return a Result Object with Outliers score and Clusters
- [ ] Examples
  - [ ] Iris flower data set (with pearson correlation)
  - [X] Reddit articles/comments classification based on text (with Cosine Similarity and Principal Component Analysis to reduce the vector length)
- [ ] Nuget package

# Copyright and license
Code released under the MIT license.
