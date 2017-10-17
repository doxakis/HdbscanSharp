using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Hdbscanstar
{
	/// <summary>
	/// An undirected graph, with weights assigned to each edge.
	/// Vertices in the graph are 0 indexed.
	/// </summary>
	public class UndirectedGraph
	{
		private int NumVertices;
		private int[] VerticesA;
		private int[] VerticesB;
		private double[] EdgeWeights;
		private List<int>[] Edges;

		/// <summary>
		/// Constructs a new UndirectedGraph, including creating an edge list for each vertex from the 
		/// vertex arrays.  For an index i, verticesA[i] and verticesB[i] share an edge with weight
		/// edgeWeights[i].
		/// </summary>
		/// <param name="numVertices">The number of vertices in the graph (indexed 0 to numVertices-1)</param>
		/// <param name="verticesA">An array of vertices corresponding to the array of edges</param>
		/// <param name="verticesB">An array of vertices corresponding to the array of edges</param>
		/// <param name="edgeWeights">An array of edges corresponding to the arrays of vertices</param>
		public UndirectedGraph(int numVertices, int[] verticesA, int[] verticesB, double[] edgeWeights)
		{
			this.NumVertices = numVertices;
			this.VerticesA = verticesA;
			this.VerticesB = verticesB;
			this.EdgeWeights = edgeWeights;
			this.Edges = new List<int>[numVertices];

			for (int i = 0; i < this.Edges.Length; i++)
			{
				this.Edges[i] = new List<int>(1 + edgeWeights.Length / numVertices);
			}

			for (int i = 0; i < edgeWeights.Length; i++)
			{
				int vertexOne = this.VerticesA[i];
				int vertexTwo = this.VerticesB[i];

				this.Edges[vertexOne].Add(vertexTwo);

				if (vertexOne != vertexTwo)
					this.Edges[vertexTwo].Add(vertexOne);
			}
		}

		/// <summary>
		/// Quicksorts the graph by edge weight in descending order.
		/// This quicksort implementation is iterative and in-place.
		/// </summary>
		public void QuicksortByEdgeWeight()
		{
			if (this.EdgeWeights.Length <= 1)
				return;

			int[] startIndexStack = new int[this.EdgeWeights.Length / 2];
			int[] endIndexStack = new int[this.EdgeWeights.Length / 2];

			startIndexStack[0] = 0;
			endIndexStack[0] = this.EdgeWeights.Length - 1;

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

		/// <summary>
		/// Quicksorts the graph in the interval [startIndex, endIndex] by edge weight.
		/// </summary>
		/// <param name="startIndex">The lowest index to be included in the sort</param>
		/// <param name="endIndex">The highest index to be included in the sort</param>
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

		/// <summary>
		/// Returns a pivot index by finding the median of edge weights between the startIndex, endIndex,
		/// and middle.
		/// </summary>
		/// <param name="startIndex">The lowest index from which the pivot index should come</param>
		/// <param name="endIndex">The highest index from which the pivot index should come</param>
		/// <returns>A pivot index</returns>
		private int SelectPivotIndex(int startIndex, int endIndex)
		{
			if (startIndex - endIndex <= 1)
				return startIndex;

			double first = this.EdgeWeights[startIndex];
			double middle = this.EdgeWeights[startIndex + (endIndex - startIndex) / 2];
			double last = this.EdgeWeights[endIndex];

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

		/// <summary>
		/// Partitions the array in the interval [startIndex, endIndex] around the value at pivotIndex.
		/// </summary>
		/// <param name="startIndex">The lowest index to  partition</param>
		/// <param name="endIndex">The highest index to partition</param>
		/// <param name="pivotIndex">The index of the edge weight to partition around</param>
		/// <returns>The index position of the pivot edge weight after the partition</returns>
		private int Partition(int startIndex, int endIndex, int pivotIndex)
		{
			double pivotValue = this.EdgeWeights[pivotIndex];
			this.SwapEdges(pivotIndex, endIndex);
			int lowIndex = startIndex;
			for (int i = startIndex; i < endIndex; i++)
			{
				if (this.EdgeWeights[i] < pivotValue)
				{
					this.SwapEdges(i, lowIndex);
					lowIndex++;
				}
			}
			this.SwapEdges(lowIndex, endIndex);
			return lowIndex;
		}

		/// <summary>
		/// Swaps the vertices and edge weights between two index locations in the graph.
		/// </summary>
		/// <param name="indexOne">The first index location</param>
		/// <param name="indexTwo">The second index location</param>
		private void SwapEdges(int indexOne, int indexTwo)
		{
			if (indexOne == indexTwo)
				return;

			int tempVertexA = this.VerticesA[indexOne];
			int tempVertexB = this.VerticesB[indexOne];
			double tempEdgeDistance = this.EdgeWeights[indexOne];
			this.VerticesA[indexOne] = this.VerticesA[indexTwo];
			this.VerticesB[indexOne] = this.VerticesB[indexTwo];
			this.EdgeWeights[indexOne] = this.EdgeWeights[indexTwo];
			this.VerticesA[indexTwo] = tempVertexA;
			this.VerticesB[indexTwo] = tempVertexB;
			this.EdgeWeights[indexTwo] = tempEdgeDistance;
		}

		public int GetNumVertices()
		{
			return this.NumVertices;
		}

		public int GetNumEdges()
		{
			return this.EdgeWeights.Length;
		}

		public int GetFirstVertexAtIndex(int index)
		{
			return this.VerticesA[index];
		}

		public int GetSecondVertexAtIndex(int index)
		{
			return this.VerticesB[index];
		}

		public double GetEdgeWeightAtIndex(int index)
		{
			return this.EdgeWeights[index];
		}

		public List<int> GetEdgeListForVertex(int vertex)
		{
			return this.Edges[vertex];
		}
	}
}
