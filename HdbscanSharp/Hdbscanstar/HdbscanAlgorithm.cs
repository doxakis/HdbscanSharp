using System;
using System.Collections.Generic;
using System.Linq;
using HdbscanSharp.Utils;

namespace HdbscanSharp.Hdbscanstar
{
	public class HdbscanAlgorithm
	{
		/// <summary>
		/// Calculates the core distances for each point in the data set, given some value for k.
		/// </summary>
		/// <param name="distances">The function to get the distance</param>
		/// <param name="numPoints">The number of elements in dataset</param>
		/// <param name="k">Each point's core distance will be it's distance to the kth nearest neighbor</param>
		/// <returns> An array of core distances</returns>
		public static double[] CalculateCoreDistances(
			Func<int, int, double> distances,
			int numPoints,
			int k)
		{
			var numNeighbors = k - 1;
			var coreDistances = new double[numPoints];

			if (k == 1)
			{
				for (var point = 0; point < numPoints; point++)
				{
					coreDistances[point] = 0;
				}
				return coreDistances;
			}

			for (var point = 0; point < numPoints; point++)
			{
				var kNNDistances = new double[numNeighbors];   //Sorted nearest distances found so far
				for (var i = 0; i < numNeighbors; i++)
				{
					kNNDistances[i] = double.MaxValue;
				}

				for (var neighbor = 0; neighbor < numPoints; neighbor++)
				{
					if (point == neighbor)
						continue;

                    var distance = distances(point, neighbor);
                    
					//Check at which position in the nearest distances the current distance would fit:
					var neighborIndex = numNeighbors;
					while (neighborIndex >= 1 && distance < kNNDistances[neighborIndex - 1])
					{
						neighborIndex--;
					}

					//Shift elements in the array to make room for the current distance:
					if (neighborIndex < numNeighbors)
					{
						for (var shiftIndex = numNeighbors - 1; shiftIndex > neighborIndex; shiftIndex--)
						{
							kNNDistances[shiftIndex] = kNNDistances[shiftIndex - 1];
						}
						kNNDistances[neighborIndex] = distance;
					}
				}
				coreDistances[point] = kNNDistances[numNeighbors - 1];
			}
			return coreDistances;
		}

        /// <summary>
        /// Constructs the minimum spanning tree of mutual reachability distances for the data set, given
        /// the core distances for each point.
        /// </summary>
        /// <param name="distances">The function to get the distance</param>
        /// <param name="numPoints">The number of elements in dataset</param>
        /// <param name="coreDistances">An array of core distances for each data point</param>
        /// <param name="selfEdges">If each point should have an edge to itself with weight equal to core distance</param>
        /// <returns> An MST for the data set using the mutual reachability distances</returns>
        public static UndirectedGraph ConstructMst(
	        Func<int, int, double> distances,
	        int numPoints,
			double[] coreDistances,
			bool selfEdges)
		{
			var selfEdgeCapacity = 0;
			if (selfEdges)
				selfEdgeCapacity = numPoints;

			//One bit is set (true) for each attached point, or unset (false) for unattached points:
			var attachedPoints = new BitSet();

			//Each point has a current neighbor point in the tree, and a current nearest distance:
			var nearestMRDNeighbors = new int[numPoints - 1 + selfEdgeCapacity];
			var nearestMRDDistances = new double[numPoints - 1 + selfEdgeCapacity];

			for (var i = 0; i < numPoints - 1; i++)
			{
				nearestMRDDistances[i] = double.MaxValue;
			}

			//The MST is expanded starting with the last point in the data set:
			var currentPoint = numPoints - 1;
			var numAttachedPoints = 1;
			attachedPoints.Set(numPoints - 1);

			//Continue attaching points to the MST until all points are attached:
			while (numAttachedPoints < numPoints)
			{
				var nearestMRDPoint = -1;
				var nearestMRDDistance = double.MaxValue;

				//Iterate through all unattached points, updating distances using the current point:
				for (var neighbor = 0; neighbor < numPoints; neighbor++)
				{
					if (currentPoint == neighbor)
						continue;

					if (attachedPoints.Get(neighbor))
						continue;

                    var distance = distances(currentPoint, neighbor);
					var mutualReachabiltiyDistance = distance;

					if (coreDistances[currentPoint] > mutualReachabiltiyDistance)
						mutualReachabiltiyDistance = coreDistances[currentPoint];

					if (coreDistances[neighbor] > mutualReachabiltiyDistance)
						mutualReachabiltiyDistance = coreDistances[neighbor];

					if (mutualReachabiltiyDistance < nearestMRDDistances[neighbor])
					{
						nearestMRDDistances[neighbor] = mutualReachabiltiyDistance;
						nearestMRDNeighbors[neighbor] = currentPoint;
					}

					//Check if the unattached point being updated is the closest to the tree:
					if (nearestMRDDistances[neighbor] <= nearestMRDDistance)
					{
						nearestMRDDistance = nearestMRDDistances[neighbor];
						nearestMRDPoint = neighbor;
					}
				}

				//Attach the closest point found in this iteration to the tree:
				attachedPoints.Set(nearestMRDPoint);
				numAttachedPoints++;
				currentPoint = nearestMRDPoint;
			}

			//Create an array for vertices in the tree that each point attached to:
			var otherVertexIndices = new int[numPoints - 1 + selfEdgeCapacity];
			for (var i = 0; i < numPoints - 1; i++)
			{
				otherVertexIndices[i] = i;
			}

			//If necessary, attach self edges:
			if (selfEdges)
			{
				for (var i = numPoints - 1; i < numPoints * 2 - 1; i++)
				{
					var vertex = i - (numPoints - 1);
					nearestMRDNeighbors[i] = vertex;
					otherVertexIndices[i] = vertex;
					nearestMRDDistances[i] = coreDistances[vertex];
				}
			}

			return new UndirectedGraph(numPoints, nearestMRDNeighbors, otherVertexIndices, nearestMRDDistances);
		}

		/// <summary>
		/// Computes the hierarchy and cluster tree from the minimum spanning tree, writing both to file, 
		/// and returns the cluster tree.  Additionally, the level at which each point becomes noise is
		/// computed.  Note that the minimum spanning tree may also have self edges (meaning it is not
		/// a true MST).
		/// </summary>
		/// <param name="mst">A minimum spanning tree which has been sorted by edge weight in descending order</param>
		/// <param name="minClusterSize">The minimum number of points which a cluster needs to be a valid cluster</param>
		/// <param name="constraints">An optional List of Constraints to calculate cluster constraint satisfaction</param>
		/// <param name="hierarchy">The hierarchy output</param>
		/// <param name="pointNoiseLevels">A double[] to be filled with the levels at which each point becomes noise</param>
		/// <param name="pointLastClusters">An int[] to be filled with the last label each point had before becoming noise</param>
		/// <returns>The cluster tree</returns>
		public static List<Cluster> ComputeHierarchyAndClusterTree(
			UndirectedGraph mst,
			int minClusterSize,
			List<HdbscanConstraint> constraints,
			List<int[]> hierarchy,
			double[] pointNoiseLevels,
			int[] pointLastClusters)
		{
			int hierarchyPosition = 0;

			//The current edge being removed from the MST:
			var currentEdgeIndex = mst.GetNumEdges() - 1;
			var nextClusterLabel = 2;
			var nextLevelSignificant = true;

			//The previous and current cluster numbers of each point in the data set:
			var previousClusterLabels = new int[mst.GetNumVertices()];
			var currentClusterLabels = new int[mst.GetNumVertices()];

			for (var i = 0; i < currentClusterLabels.Length; i++)
			{
				currentClusterLabels[i] = 1;
				previousClusterLabels[i] = 1;
			}

			//A list of clusters in the cluster tree, with the 0th cluster (noise) null:
			var clusters = new List<Cluster>();
			clusters.Add(null);
			clusters.Add(new Cluster(1, null, double.NaN, mst.GetNumVertices()));

			//Calculate number of constraints satisfied for cluster 1:
			var clusterOne = new SortedSet<int>();
			clusterOne.Add(1);
			CalculateNumConstraintsSatisfied(
				clusterOne,
				clusters,
				constraints,
				currentClusterLabels);

			//Sets for the clusters and vertices that are affected by the edge(s) being removed:
			var affectedClusterLabels = new SortedSet<int>();
			var affectedVertices = new SortedSet<int>();

			while (currentEdgeIndex >= 0)
			{
				var currentEdgeWeight = mst.GetEdgeWeightAtIndex(currentEdgeIndex);
				var newClusters = new List<Cluster>();

				//Remove all edges tied with the current edge weight, and store relevant clusters and vertices:
				while (currentEdgeIndex >= 0 && mst.GetEdgeWeightAtIndex(currentEdgeIndex) == currentEdgeWeight)
				{
					var firstVertex = mst.GetFirstVertexAtIndex(currentEdgeIndex);
					var secondVertex = mst.GetSecondVertexAtIndex(currentEdgeIndex);
					mst.GetEdgeListForVertex(firstVertex).Remove(secondVertex);
					mst.GetEdgeListForVertex(secondVertex).Remove(firstVertex);

					if (currentClusterLabels[firstVertex] == 0)
					{
						currentEdgeIndex--;
						continue;
					}
					affectedVertices.Add(firstVertex);
					affectedVertices.Add(secondVertex);
					affectedClusterLabels.Add(currentClusterLabels[firstVertex]);
					currentEdgeIndex--;
				}

				if (!affectedClusterLabels.Any())
					continue;

				//Check each cluster affected for a possible split:
				while (affectedClusterLabels.Any())
				{
					var examinedClusterLabel = affectedClusterLabels.Last();
					affectedClusterLabels.Remove(examinedClusterLabel);

					var examinedVertices = new SortedSet<int>();

					//Get all affected vertices that are members of the cluster currently being examined:
					foreach (var vertex in affectedVertices.ToList())
					{
						if (currentClusterLabels[vertex] == examinedClusterLabel)
						{
							examinedVertices.Add(vertex);
							affectedVertices.Remove(vertex);
						}
					}

					SortedSet<int> firstChildCluster = null;
					LinkedList<int> unexploredFirstChildClusterPoints = null;
					var numChildClusters = 0;

					/*
					 * Check if the cluster has split or shrunk by exploring the graph from each affected
					 * vertex.  If there are two or more valid child clusters (each has >= minClusterSize
					 * points), the cluster has split.
					 * Note that firstChildCluster will only be fully explored if there is a cluster
					 * split, otherwise, only spurious components are fully explored, in order to label 
					 * them noise.
					 */
					while (examinedVertices.Any())
					{
						var constructingSubCluster = new SortedSet<int>();
						var unexploredSubClusterPoints = new LinkedList<int>();
						var anyEdges = false;
						var incrementedChildCount = false;
						var rootVertex = examinedVertices.Last();
						constructingSubCluster.Add(rootVertex);
						unexploredSubClusterPoints.AddLast(rootVertex);
						examinedVertices.Remove(rootVertex);

						//Explore this potential child cluster as long as there are unexplored points:
						while (unexploredSubClusterPoints.Any())
						{
							var vertexToExplore = unexploredSubClusterPoints.First();
							unexploredSubClusterPoints.RemoveFirst();

							foreach (var neighbor in mst.GetEdgeListForVertex(vertexToExplore))
							{
								anyEdges = true;
								if (constructingSubCluster.Add(neighbor))
								{
									unexploredSubClusterPoints.AddLast(neighbor);
									examinedVertices.Remove(neighbor);
								}
							}

							//Check if this potential child cluster is a valid cluster:
							if (!incrementedChildCount && constructingSubCluster.Count >= minClusterSize && anyEdges)
							{
								incrementedChildCount = true;
								numChildClusters++;

								//If this is the first valid child cluster, stop exploring it:
								if (firstChildCluster == null)
								{
									firstChildCluster = constructingSubCluster;
									unexploredFirstChildClusterPoints = unexploredSubClusterPoints;
									break;
								}
							}
						}

						//If there could be a split, and this child cluster is valid:
						if (numChildClusters >= 2 && constructingSubCluster.Count >= minClusterSize && anyEdges)
						{
							//Check this child cluster is not equal to the unexplored first child cluster:
							var firstChildClusterMember = firstChildCluster.Last();
							if (constructingSubCluster.Contains(firstChildClusterMember))
								numChildClusters--;
							//Otherwise, create a new cluster:
							else
							{
								var newCluster = CreateNewCluster(constructingSubCluster, currentClusterLabels,
										clusters[examinedClusterLabel], nextClusterLabel, currentEdgeWeight);
								newClusters.Add(newCluster);
								clusters.Add(newCluster);
								nextClusterLabel++;
							}
						}
						//If this child cluster is not valid cluster, assign it to noise:
						else if (constructingSubCluster.Count < minClusterSize || !anyEdges)
						{
							CreateNewCluster(constructingSubCluster, currentClusterLabels,
									clusters[examinedClusterLabel], 0, currentEdgeWeight);

							foreach (var point in constructingSubCluster)
							{
								pointNoiseLevels[point] = currentEdgeWeight;
								pointLastClusters[point] = examinedClusterLabel;
							}
						}
					}

					//Finish exploring and cluster the first child cluster if there was a split and it was not already clustered:
					if (numChildClusters >= 2 && currentClusterLabels[firstChildCluster.First()] == examinedClusterLabel)
					{
						while (unexploredFirstChildClusterPoints.Any())
						{
							var vertexToExplore = unexploredFirstChildClusterPoints.First();
							unexploredFirstChildClusterPoints.RemoveFirst();
							foreach (var neighbor in mst.GetEdgeListForVertex(vertexToExplore))
							{
								if (firstChildCluster.Add(neighbor))
									unexploredFirstChildClusterPoints.AddLast(neighbor);
							}
						}
						var newCluster = CreateNewCluster(firstChildCluster, currentClusterLabels,
								clusters[examinedClusterLabel], nextClusterLabel, currentEdgeWeight);
						newClusters.Add(newCluster);
						clusters.Add(newCluster);
						nextClusterLabel++;
					}
				}

				//Write out the current level of the hierarchy:
				if (nextLevelSignificant || newClusters.Any())
				{
					int[] lineContents = new int[previousClusterLabels.Length];
					for (var i = 0; i < previousClusterLabels.Length; i++)
						lineContents[i] = previousClusterLabels[i];
					hierarchy.Add(lineContents);
					hierarchyPosition++;
				}

				//Assign file offsets and calculate the number of constraints satisfied:
				var newClusterLabels = new SortedSet<int>();
				foreach (var newCluster in newClusters)
				{
					newCluster.HierarchyPosition = hierarchyPosition;
					newClusterLabels.Add(newCluster.Label);
				}

				if (newClusterLabels.Any())
					CalculateNumConstraintsSatisfied(newClusterLabels, clusters, constraints, currentClusterLabels);

				for (var i = 0; i < previousClusterLabels.Length; i++)
				{
					previousClusterLabels[i] = currentClusterLabels[i];
				}

				if (!newClusters.Any())
					nextLevelSignificant = false;
				else
					nextLevelSignificant = true;
			}

			//Write out the final level of the hierarchy (all points noise):
			{
				int[] lineContents = new int[previousClusterLabels.Length + 1];
				for (var i = 0; i < previousClusterLabels.Length; i++)
					lineContents[i] = 0;
				hierarchy.Add(lineContents);
			}
			
			return clusters;
		}

		/// <summary>
		/// Propagates constraint satisfaction, stability, and lowest child death level from each child
		/// cluster to each parent cluster in the tree.  This method must be called before calling
		/// findProminentClusters() or calculateOutlierScores().
		/// </summary>
		/// <param name="clusters">A list of Clusters forming a cluster tree</param>
		/// <returns>true if there are any clusters with infinite stability, false otherwise</returns>
		public static bool PropagateTree(List<Cluster> clusters)
		{
			var clustersToExamine = new SortedDictionary<int, Cluster>();
			var addedToExaminationList = new BitSet();
			var infiniteStability = false;

			//Find all leaf clusters in the cluster tree:
			foreach (var cluster in clusters)
			{
				if (cluster != null && !cluster.HasChildren)
				{
					var label = cluster.Label;
					clustersToExamine.Remove(label);
					clustersToExamine.Add(label, cluster);
					addedToExaminationList.Set(label);
				}
			}

			//Iterate through every cluster, propagating stability from children to parents:
			while (clustersToExamine.Any())
			{
				var currentKeyValue = clustersToExamine.Last();
				var currentCluster = currentKeyValue.Value;
				clustersToExamine.Remove(currentKeyValue.Key);

				currentCluster.Propagate();

				if (currentCluster.Stability == double.PositiveInfinity)
					infiniteStability = true;

				if (currentCluster.Parent != null)
				{
					var parent = currentCluster.Parent;
					var label = parent.Label;

					if (!addedToExaminationList.Get(label))
					{
						clustersToExamine.Remove(label);
						clustersToExamine.Add(label, parent);
						addedToExaminationList.Set(label);
					}
				}
			}

			return infiniteStability;
		}

		/// <summary>
		/// Produces a flat clustering result using constraint satisfaction and cluster stability, and 
		/// returns an array of labels.  propagateTree() must be called before calling this method.
		/// </summary>
		/// <param name="clusters">A list of Clusters forming a cluster tree which has already been propagated</param>
		/// <param name="hierarchy">The hierarchy content</param>
		/// <param name="numPoints">The number of points in the original data set</param>
		/// <returns>An array of labels for the flat clustering result</returns>
		public static int[] FindProminentClusters(
			List<Cluster> clusters,
			List<int[]> hierarchy,
			int numPoints)
		{
			//Take the list of propagated clusters from the root cluster:
			var solution = clusters[1].PropagatedDescendants;

			var flatPartitioning = new int[numPoints];

			//Store all the hierarchy positions at which to find the birth points for the flat clustering:
			var significantHierarchyPositions = new SortedDictionary<int, List<int>>();

			foreach (var cluster in solution)
			{
				var hierarchyPosition = cluster.HierarchyPosition;
				if (significantHierarchyPositions.ContainsKey(hierarchyPosition))
					significantHierarchyPositions[hierarchyPosition].Add(cluster.Label);
				else
					significantHierarchyPositions[hierarchyPosition] = new List<int> { cluster.Label };
			}

			//Go through the hierarchy file, setting labels for the flat clustering:
			while (significantHierarchyPositions.Any())
			{
				var entry = significantHierarchyPositions.First();
				significantHierarchyPositions.Remove(entry.Key);

				var clusterList = entry.Value;
				var hierarchyPosition = entry.Key;
				var lineContents = hierarchy[hierarchyPosition];
				
				for (var i = 0; i < lineContents.Length; i++)
				{
					var label = lineContents[i];
					if (clusterList.Contains(label))
						flatPartitioning[i] = label;
				}
			}
			return flatPartitioning;
		}

		/// <summary>
		/// Produces the outlier score for each point in the data set, and returns a sorted list of outlier
		/// scores.  propagateTree() must be called before calling this method.
		/// </summary>
		/// <param name="clusters">A list of Clusters forming a cluster tree which has already been propagated</param>
		/// <param name="pointNoiseLevels">A double[] with the levels at which each point became noise</param>
		/// <param name="pointLastClusters">An int[] with the last label each point had before becoming noise</param>
		/// <param name="coreDistances">An array of core distances for each data point</param>
		/// <returns>An List of OutlierScores, sorted in descending order</returns>
		public static List<OutlierScore> CalculateOutlierScores(
			List<Cluster> clusters,
			double[] pointNoiseLevels,
			int[] pointLastClusters,
			double[] coreDistances)
		{
			var numPoints = pointNoiseLevels.Length;
			var outlierScores = new List<OutlierScore>(numPoints);

			//Iterate through each point, calculating its outlier score:
			for (var i = 0; i < numPoints; i++)
			{
				var epsilonMax = clusters[pointLastClusters[i]].PropagatedLowestChildDeathLevel;
				var epsilon = pointNoiseLevels[i];
				double score = 0;

				if (epsilon != 0)
					score = 1 - (epsilonMax / epsilon);

				outlierScores.Add(new OutlierScore(score, coreDistances[i], i));
			}

			//Sort the outlier scores:
			outlierScores.Sort();

			return outlierScores;
		}

		/// <summary>
		/// Removes the set of points from their parent Cluster, and creates a new Cluster, provided the
		/// clusterId is not 0 (noise).
		/// </summary>
		/// <param name="points">The set of points to be in the new Cluster</param>
		/// <param name="clusterLabels">An array of cluster labels, which will be modified</param>
		/// <param name="parentCluster">The parent Cluster of the new Cluster being created</param>
		/// <param name="clusterLabel">The label of the new Cluster </param>
		/// <param name="edgeWeight">The edge weight at which to remove the points from their previous Cluster</param>
		/// <returns>The new Cluster, or null if the clusterId was 0</returns>
		private static Cluster CreateNewCluster(
			SortedSet<int> points,
			int[] clusterLabels,
			Cluster parentCluster,
			int clusterLabel,
			double edgeWeight)
		{
			foreach (var point in points)
			{
				clusterLabels[point] = clusterLabel;
			}

			parentCluster.DetachPoints(points.Count, edgeWeight);

			if (clusterLabel != 0)
				return new Cluster(clusterLabel, parentCluster, edgeWeight, points.Count);
			
			parentCluster.AddPointsToVirtualChildCluster(points);
			return null;
		}

		/// <summary>
		/// Calculates the number of constraints satisfied by the new clusters and virtual children of the
		/// parents of the new clusters.
		/// </summary>
		/// <param name="newClusterLabels">Labels of new clusters</param>
		/// <param name="clusters">An List of clusters</param>
		/// <param name="constraints">An List of constraints</param>
		/// <param name="clusterLabels">An array of current cluster labels for points</param>
		private static void CalculateNumConstraintsSatisfied(
			SortedSet<int> newClusterLabels,
			List<Cluster> clusters,
			List<HdbscanConstraint> constraints,
			int[] clusterLabels)
		{
			if (constraints == null)
				return;

			var parents = new List<Cluster>();

			foreach (var label in newClusterLabels)
			{
				var parent = clusters[label].Parent;
				if (parent != null && !parents.Contains(parent))
					parents.Add(parent);
			}

			foreach (var constraint in constraints)
			{
				var labelA = clusterLabels[constraint.GetPointA()];
				var labelB = clusterLabels[constraint.GetPointB()];

				if (constraint.GetConstraintType() == HdbscanConstraintType.MustLink && labelA == labelB)
				{
					if (newClusterLabels.Contains(labelA))
						clusters[labelA].AddConstraintsSatisfied(2);
				}
				else if (constraint.GetConstraintType() == HdbscanConstraintType.CannotLink && (labelA != labelB || labelA == 0))
				{
					if (labelA != 0 && newClusterLabels.Contains(labelA))
						clusters[labelA].AddConstraintsSatisfied(1);
					if (labelB != 0 && newClusterLabels.Contains(labelB))
						clusters[labelB].AddConstraintsSatisfied(1);
					if (labelA == 0)
					{
						foreach (var parent in parents)
						{
							if (parent.VirtualChildClusterConstraintsPoint(constraint.GetPointA()))
							{
								parent.AddVirtualChildConstraintsSatisfied(1);
								break;
							}
						}
					}
					if (labelB == 0)
					{
						foreach (var parent in parents)
						{
							if (parent.VirtualChildClusterConstraintsPoint(constraint.GetPointB()))
							{
								parent.AddVirtualChildConstraintsSatisfied(1);
								break;
							}
						}
					}
				}
			}

			foreach (var parent in parents)
			{
				parent.ReleaseVirtualChildCluster();
			}
		}
	}
}
