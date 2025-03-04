# Changelog

### 3.0.0
- [BREAKING] The class `IDistanceFunction` interface is now generic and does not have "index" parameters. The same applied for all of its implementations.
- [BREAKING] The class `HdbscanParameters` was transformed into hierarchy: `HdbscanParameters` and `HdbscanParametersSparseMatrix` are sub-classes of `HdbscanParametersBase`. All of them are generic, where type parameter designates vector coordinate.
- [BREAKING] The interface `ISparseMatrixSupport` was transformed into `ISparseMatrixDistanceFunction` for separate sparse matrix logic.
- `Dataset` now can be `float[][]` or `Dictionary<int, float>[]`

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
