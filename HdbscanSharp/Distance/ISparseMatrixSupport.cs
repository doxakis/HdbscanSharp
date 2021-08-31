namespace HdbscanSharp.Distance
{
    public interface ISparseMatrixSupport
    {
        /// <summary>
        /// Indicate the most common distance value for sparse matrix.
        /// </summary>
        double GetMostCommonDistanceValueForSparseMatrix();
    }
}