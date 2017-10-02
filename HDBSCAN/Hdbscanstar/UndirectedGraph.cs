using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDBSCAN.Hdbscanstar
{
	/**
	 * An undirected graph, with weights assigned to each edge.  Vertices in the graph are 0 indexed.
	 */
	public class UndirectedGraph
	{
		private int numVertices;
		private int[] verticesA;
		private int[] verticesB;
		private double[] edgeWeights;
		private List<int>[] edges;     //Each Object in this array in an List<int>

		/**
		 * Constructs a new UndirectedGraph, including creating an edge list for each vertex from the 
		 * vertex arrays.  For an index i, verticesA[i] and verticesB[i] share an edge with weight
		 * edgeWeights[i].
		 * @param numVertices The number of vertices in the graph (indexed 0 to numVertices-1)
		 * @param verticesA An array of vertices corresponding to the array of edges
		 * @param verticesB An array of vertices corresponding to the array of edges
		 * @param edgeWeights An array of edges corresponding to the arrays of vertices
		 */
		public UndirectedGraph(int numVertices, int[] verticesA, int[] verticesB, double[] edgeWeights)
		{
			this.numVertices = numVertices;
			this.verticesA = verticesA;
			this.verticesB = verticesB;
			this.edgeWeights = edgeWeights;
			this.edges = new List<int>[numVertices];

			for (int i = 0; i < this.edges.Length; i++)
			{
				this.edges[i] = new List<int>(1 + edgeWeights.Length / numVertices);
			}

			for (int i = 0; i < edgeWeights.Length; i++)
			{
				int vertexOne = this.verticesA[i];
				int vertexTwo = this.verticesB[i];

				this.edges[vertexOne].Add(vertexTwo);

				if (vertexOne != vertexTwo)
					this.edges[vertexTwo].Add(vertexOne);
			}
		}

		/**
		 * Quicksorts the graph by edge weight in descending order.  This quicksort implementation is 
		 * iterative and in-place.
		 */
		public void QuicksortByEdgeWeight()
		{
			if (this.edgeWeights.Length <= 1)
				return;

			int[] startIndexStack = new int[this.edgeWeights.Length / 2];
			int[] endIndexStack = new int[this.edgeWeights.Length / 2];

			startIndexStack[0] = 0;
			endIndexStack[0] = this.edgeWeights.Length - 1;

			int stackTop = 0;

			while (stackTop >= 0)
			{
				int startIndex = startIndexStack[stackTop];
				int endIndex = endIndexStack[stackTop];
				stackTop--;

				int pivotIndex = this.SelectPivotIndex(startIndex, endIndex);
				pivotIndex = this.Partition(startIndex, endIndex, pivotIndex);

				if (pivotIndex > startIndex + 1)
				{
					startIndexStack[stackTop + 1] = startIndex;
					endIndexStack[stackTop + 1] = pivotIndex - 1;
					stackTop++;
				}

				if (pivotIndex < endIndex - 1)
				{
					startIndexStack[stackTop + 1] = pivotIndex + 1;
					endIndexStack[stackTop + 1] = endIndex;
					stackTop++;
				}
			}
		}

		/**
		 * Quicksorts the graph in the interval [startIndex, endIndex] by edge weight.
		 * @param startIndex The lowest index to be included in the sort
		 * @param endIndex The highest index to be included in the sort
		 */
		private void Quicksort(int startIndex, int endIndex)
		{
			if (startIndex < endIndex)
			{
				int pivotIndex = this.SelectPivotIndex(startIndex, endIndex);
				pivotIndex = this.Partition(startIndex, endIndex, pivotIndex);
				this.Quicksort(startIndex, pivotIndex - 1);
				this.Quicksort(pivotIndex + 1, endIndex);
			}
		}

		/**
		 * Returns a pivot index by finding the median of edge weights between the startIndex, endIndex,
		 * and middle.
		 * @param startIndex The lowest index from which the pivot index should come
		 * @param endIndex The highest index from which the pivot index should come
		 * @return A pivot index
		 */
		private int SelectPivotIndex(int startIndex, int endIndex)
		{
			if (startIndex - endIndex <= 1)
				return startIndex;

			double first = this.edgeWeights[startIndex];
			double middle = this.edgeWeights[startIndex + (endIndex - startIndex) / 2];
			double last = this.edgeWeights[endIndex];

			if (first <= middle)
			{
				if (middle <= last)
					return startIndex + (endIndex - startIndex) / 2;
				else if (last >= first)
					return endIndex;
				else
					return startIndex;
			}
			else
			{
				if (first <= last)
					return startIndex;
				else if (last >= middle)
					return endIndex;
				else
					return startIndex + (endIndex - startIndex) / 2;
			}
		}

		/**
		 * Partitions the array in the interval [startIndex, endIndex] around the value at pivotIndex.
		 * @param startIndex The lowest index to  partition
		 * @param endIndex The highest index to partition
		 * @param pivotIndex The index of the edge weight to partition around
		 * @return The index position of the pivot edge weight after the partition
		 */
		private int Partition(int startIndex, int endIndex, int pivotIndex)
		{
			double pivotValue = this.edgeWeights[pivotIndex];
			this.SwapEdges(pivotIndex, endIndex);
			int lowIndex = startIndex;
			for (int i = startIndex; i < endIndex; i++)
			{
				if (this.edgeWeights[i] < pivotValue)
				{
					this.SwapEdges(i, lowIndex);
					lowIndex++;
				}
			}
			this.SwapEdges(lowIndex, endIndex);
			return lowIndex;
		}

		/**
		 * Swaps the vertices and edge weights between two index locations in the graph.
		 * @param indexOne The first index location
		 * @param indexTwo The second index location
		 */
		private void SwapEdges(int indexOne, int indexTwo)
		{
			if (indexOne == indexTwo)
				return;

			int tempVertexA = this.verticesA[indexOne];
			int tempVertexB = this.verticesB[indexOne];
			double tempEdgeDistance = this.edgeWeights[indexOne];
			this.verticesA[indexOne] = this.verticesA[indexTwo];
			this.verticesB[indexOne] = this.verticesB[indexTwo];
			this.edgeWeights[indexOne] = this.edgeWeights[indexTwo];
			this.verticesA[indexTwo] = tempVertexA;
			this.verticesB[indexTwo] = tempVertexB;
			this.edgeWeights[indexTwo] = tempEdgeDistance;
		}

		public int GetNumVertices()
		{
			return this.numVertices;
		}

		public int GetNumEdges()
		{
			return this.edgeWeights.Length;
		}

		public int GetFirstVertexAtIndex(int index)
		{
			return this.verticesA[index];
		}

		public int GetSecondVertexAtIndex(int index)
		{
			return this.verticesB[index];
		}

		public double GetEdgeWeightAtIndex(int index)
		{
			return this.edgeWeights[index];
		}

		public List<int> GetEdgeListForVertex(int vertex)
		{
			return this.edges[vertex];
		}
	}
}
