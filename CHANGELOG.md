# Changelog

### 3.0.1
- Add new overloaded function of HdbscanRunner.Run so it is easier to perform segmentation on classes
- Add new interface IHdbscanRunner and class HdbscanRunnerInstance. So it is possible to mock the algorithm for unit testing purpose.

### 3.0.0
- Add GenericCosineSimilarity and GenericEuclideanDistance to allow generic math operation and use hardware acceleration (Advanced Vector Extensions, aka AVX) when available
- [BREAKING] HdbscanParameters has been removed and replaced with arguments to HdbscanRunner.Run in order to allow generic math operation

for example, the following code:
```
var result = HdbscanRunner.Run(new HdbscanParameters
{
    DataSet = dataset,
    MinPoints = 25,
    MinClusterSize = 25,
    CacheDistance = true,
    MaxDegreeOfParallelism = 1,
    DistanceFunction = new CosineSimilarity()
});
```

can be changed to:
```
var result = HdbscanRunner.Run(
    dataset.Length, // The number of element in the dataset
    25, // MinPoints
    25, // MinClusterSize
    DistanceHelpers.GetFunc(new CosineSimilarity(
        false, // use caching for distance
        false), // specify if used with multiple threads
    dataset, // double[][] for normal matrix or Dictionary<int, int>[] for sparse matrix
    null,
    true, // use caching for distance
    1)); // to indicate to use all threads, you can specify 0.
```

### 2.0.0
- [BREAKING] Remove parameter `UseMultipleThread` since we can use only the parameter `MaxDegreeOfParallelism`.
- [BREAKING] The class `HdbscanParameters` accept a type parameter to allow indicating the dataset. (`double[]` for normal matrix or `Dictionary<int, int>` for sparse matrix)
- The parameter `DataSet` can be a `Dictionary<int, int>[]` for sparse matrix
- Remove project DocumentClusteringExample (will be moved to another repo and rewritten in a cleaner way)
- Remove project OrderByMostSimilarDocumentExample (since not really useful)
- Add support for sparse matrix (only `CosineSimilarity` is available for now)
- Improve performance of `CosineSimilarity` and make it possible to use caching to reduce redoing same calculation multiple times.
- Add new parameter `CacheDistance` to allow controlling the caching. Before that, it was caching distances and it could cause issues with big dataset by using to much memory.
- Fix various warnings.

### 1.0.X
- Initial releases
