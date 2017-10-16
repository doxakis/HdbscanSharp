﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HdbscanSharp.Distance;
using HdbscanSharp.Hdbscanstar;
using HdbscanSharp.Utils;
using System.Globalization;

namespace HdbscanSharp.Hdbscanstar
{
	public class HdbscanAlgorithm
	{
		/**
		 * Calculates the core distances for each point in the data set, given some value for k.
		 * @param dataSet A double[][] where index [i][j] indicates the jth attribute of data point i
		 * @param k Each point's core distance will be it's distance to the kth nearest neighbor
		 * @param distanceFunction A DistanceCalculator to compute distances between points
		 * @return An array of core distances
		 */
		public static double[] CalculateCoreDistances(
			double[][] dataSet,
			int k,
			IDistanceCalculator distanceFunction)
		{
			int numNeighbors = k - 1;
			double[] coreDistances = new double[dataSet.Length];

			if (k == 1)
			{
				for (int point = 0; point < dataSet.Length; point++)
				{
					coreDistances[point] = 0;
				}
				return coreDistances;
			}

			for (int point = 0; point < dataSet.Length; point++)
			{
				double[] kNNDistances = new double[numNeighbors];   //Sorted nearest distances found so far
				for (int i = 0; i < numNeighbors; i++)
				{
					kNNDistances[i] = double.MaxValue;
				}

				for (int neighbor = 0; neighbor < dataSet.Length; neighbor++)
				{
					if (point == neighbor)
						continue;

					double distance = distanceFunction.ComputeDistance(dataSet[point], dataSet[neighbor]);

					//Check at which position in the nearest distances the current distance would fit:
					int neighborIndex = numNeighbors;
					while (neighborIndex >= 1 && distance < kNNDistances[neighborIndex - 1])
					{
						neighborIndex--;
					}

					//Shift elements in the array to make room for the current distance:
					if (neighborIndex < numNeighbors)
					{
						for (int shiftIndex = numNeighbors - 1; shiftIndex > neighborIndex; shiftIndex--)
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

		/**
		 * Constructs the minimum spanning tree of mutual reachability distances for the data set, given
		 * the core distances for each point.
		 * @param dataSet A double[][] where index [i][j] indicates the jth attribute of data point i
		 * @param coreDistances An array of core distances for each data point
		 * @param selfEdges If each point should have an edge to itself with weight equal to core distance
		 * @param distanceFunction A DistanceCalculator to compute distances between points
		 * @return An MST for the data set using the mutual reachability distances
		 */
		public static UndirectedGraph ConstructMST(
			double[][] dataSet,
			double[] coreDistances,
			bool selfEdges,
			IDistanceCalculator distanceFunction)
		{
			int selfEdgeCapacity = 0;
			if (selfEdges)
				selfEdgeCapacity = dataSet.Length;

			//One bit is set (true) for each attached point, or unset (false) for unattached points:
			BitSet attachedPoints = new BitSet();

			//Each point has a current neighbor point in the tree, and a current nearest distance:
			int[] nearestMRDNeighbors = new int[dataSet.Length - 1 + selfEdgeCapacity];
			double[] nearestMRDDistances = new double[dataSet.Length - 1 + selfEdgeCapacity];

			for (int i = 0; i < dataSet.Length - 1; i++)
			{
				nearestMRDDistances[i] = double.MaxValue;
			}

			//The MST is expanded starting with the last point in the data set:
			int currentPoint = dataSet.Length - 1;
			int numAttachedPoints = 1;
			attachedPoints.Set(dataSet.Length - 1);

			//Continue attaching points to the MST until all points are attached:
			while (numAttachedPoints < dataSet.Length)
			{
				int nearestMRDPoint = -1;
				double nearestMRDDistance = double.MaxValue;

				//Iterate through all unattached points, updating distances using the current point:
				for (int neighbor = 0; neighbor < dataSet.Length; neighbor++)
				{
					if (currentPoint == neighbor)
						continue;

					if (attachedPoints.Get(neighbor) == true)
						continue;

					double distance = distanceFunction.ComputeDistance(dataSet[currentPoint], dataSet[neighbor]);
					double mutualReachabiltiyDistance = distance;

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
			int[] otherVertexIndices = new int[dataSet.Length - 1 + selfEdgeCapacity];
			for (int i = 0; i < dataSet.Length - 1; i++)
			{
				otherVertexIndices[i] = i;
			}

			//If necessary, attach self edges:
			if (selfEdges)
			{
				for (int i = dataSet.Length - 1; i < dataSet.Length * 2 - 1; i++)
				{
					int vertex = i - (dataSet.Length - 1);
					nearestMRDNeighbors[i] = vertex;
					otherVertexIndices[i] = vertex;
					nearestMRDDistances[i] = coreDistances[vertex];
				}
			}

			return new UndirectedGraph(dataSet.Length, nearestMRDNeighbors, otherVertexIndices, nearestMRDDistances);
		}

		/**
		 * Computes the hierarchy and cluster tree from the minimum spanning tree, writing both to file, 
		 * and returns the cluster tree.  Additionally, the level at which each point becomes noise is
		 * computed.  Note that the minimum spanning tree may also have self edges (meaning it is not
		 * a true MST).
		 * @param mst A minimum spanning tree which has been sorted by edge weight in descending order
		 * @param minClusterSize The minimum number of points which a cluster needs to be a valid cluster
		 * @param compactHierarchy Indicates if hierarchy should include all levels or only levels at 
		 * which clusters first appear
		 * @param constraints An optional List of Constraints to calculate cluster constraint satisfaction
		 * @param hierarchyOutputFile The path to the hierarchy output file
		 * @param treeOutputFile The path to the cluster tree output file
		 * @param delimiter The delimiter to be used while writing both files
		 * @param pointNoiseLevels A double[] to be filled with the levels at which each point becomes noise
		 * @param pointLastClusters An int[] to be filled with the last label each point had before becoming noise
		 * @return The cluster tree
		 * @throws IOException If any errors occur opening or writing to the files
		 */
		public static List<Cluster> ComputeHierarchyAndClusterTree(
			UndirectedGraph mst,
			int minClusterSize,
			bool compactHierarchy,
			List<HdbscanConstraint> constraints,
			StringBuilder hierarchyWriter,
			char delimiter,
			double[] pointNoiseLevels,
			int[] pointLastClusters)
		{
			long hierarchyCharsWritten = 0;
			int lineCount = 0; //Indicates the number of lines written into hierarchyFile.

			//The current edge being removed from the MST:
			int currentEdgeIndex = mst.GetNumEdges() - 1;
			int nextClusterLabel = 2;
			bool nextLevelSignificant = true;

			//The previous and current cluster numbers of each point in the data set:
			int[] previousClusterLabels = new int[mst.GetNumVertices()];
			int[] currentClusterLabels = new int[mst.GetNumVertices()];

			for (int i = 0; i < currentClusterLabels.Length; i++)
			{
				currentClusterLabels[i] = 1;
				previousClusterLabels[i] = 1;
			}

			//A list of clusters in the cluster tree, with the 0th cluster (noise) null:
			List<Cluster> clusters = new List<Cluster>();
			clusters.Add(null);
			clusters.Add(new Cluster(1, null, Double.NaN, mst.GetNumVertices()));

			//Calculate number of constraints satisfied for cluster 1:
			SortedSet<int> clusterOne = new SortedSet<int>();
			clusterOne.Add(1);
			CalculateNumConstraintsSatisfied(
				clusterOne,
				clusters,
				constraints,
				currentClusterLabels);

			//Sets for the clusters and vertices that are affected by the edge(s) being removed:
			SortedSet<int> affectedClusterLabels = new SortedSet<int>();
			SortedSet<int> affectedVertices = new SortedSet<int>();

			while (currentEdgeIndex >= 0)
			{
				double currentEdgeWeight = mst.GetEdgeWeightAtIndex(currentEdgeIndex);
				List<Cluster> newClusters = new List<Cluster>();

				//Remove all edges tied with the current edge weight, and store relevant clusters and vertices:
				while (currentEdgeIndex >= 0 && mst.GetEdgeWeightAtIndex(currentEdgeIndex) == currentEdgeWeight)
				{
					int firstVertex = mst.GetFirstVertexAtIndex(currentEdgeIndex);
					int secondVertex = mst.GetSecondVertexAtIndex(currentEdgeIndex);
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
					int examinedClusterLabel = affectedClusterLabels.Last();
					affectedClusterLabels.Remove(examinedClusterLabel);

					SortedSet<int> examinedVertices = new SortedSet<int>();

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
					int numChildClusters = 0;

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
						SortedSet<int> constructingSubCluster = new SortedSet<int>();
						LinkedList<int> unexploredSubClusterPoints = new LinkedList<int>();
						bool anyEdges = false;
						bool incrementedChildCount = false;
						int rootVertex = examinedVertices.Last();
						constructingSubCluster.Add(rootVertex);
						unexploredSubClusterPoints.AddLast(rootVertex);
						examinedVertices.Remove(rootVertex);

						//Explore this potential child cluster as long as there are unexplored points:
						while (unexploredSubClusterPoints.Any())
						{
							int vertexToExplore = unexploredSubClusterPoints.First();
							unexploredSubClusterPoints.RemoveFirst();

							foreach (int neighbor in mst.GetEdgeListForVertex(vertexToExplore))
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
							int firstChildClusterMember = firstChildCluster.Last();
							if (constructingSubCluster.Contains(firstChildClusterMember))
								numChildClusters--;
							//Otherwise, create a new cluster:
							else
							{
								Cluster newCluster = CreateNewCluster(constructingSubCluster, currentClusterLabels,
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

							foreach (int point in constructingSubCluster)
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
							int vertexToExplore = unexploredFirstChildClusterPoints.First();
							unexploredFirstChildClusterPoints.RemoveFirst();
							foreach (int neighbor in mst.GetEdgeListForVertex(vertexToExplore))
							{
								if (firstChildCluster.Add(neighbor))
									unexploredFirstChildClusterPoints.AddLast(neighbor);
							}
						}
						Cluster newCluster = CreateNewCluster(firstChildCluster, currentClusterLabels,
								clusters[examinedClusterLabel], nextClusterLabel, currentEdgeWeight);
						newClusters.Add(newCluster);
						clusters.Add(newCluster);
						nextClusterLabel++;
					}
				}

				//Write out the current level of the hierarchy:
				if (!compactHierarchy || nextLevelSignificant || newClusters.Any())
				{
					int outputLength = 0;
					string output = currentEdgeWeight + "" + delimiter;
					hierarchyWriter.Append(output);
					outputLength += output.Length;

					for (int i = 0; i < previousClusterLabels.Length - 1; i++)
					{
						output = previousClusterLabels[i] + "" + delimiter;
						hierarchyWriter.Append(output);
						outputLength += output.Length;
					}
					output = previousClusterLabels[previousClusterLabels.Length - 1] + "\n";
					hierarchyWriter.Append(output);
					outputLength += output.Length;
					lineCount++;
					hierarchyCharsWritten += outputLength;
				}

				//Assign file offsets and calculate the number of constraints satisfied:
				SortedSet<int> newClusterLabels = new SortedSet<int>();
				foreach (Cluster newCluster in newClusters)
				{
					newCluster.SetFileOffset(hierarchyCharsWritten);
					newClusterLabels.Add(newCluster.GetLabel());
				}

				if (newClusterLabels.Any())
					CalculateNumConstraintsSatisfied(newClusterLabels, clusters, constraints, currentClusterLabels);

				for (int i = 0; i < previousClusterLabels.Length; i++)
				{
					previousClusterLabels[i] = currentClusterLabels[i];
				}

				if (!newClusters.Any())
					nextLevelSignificant = false;
				else
					nextLevelSignificant = true;
			}

			//Write out the final level of the hierarchy (all points noise):
			hierarchyWriter.Append(0 + "" + delimiter);
			for (int i = 0; i < previousClusterLabels.Length - 1; i++)
			{
				hierarchyWriter.Append(0 + "" + delimiter);
			}
			hierarchyWriter.Append(0 + "\n");
			lineCount++;
			return clusters;
		}

		/**
		 * Propagates constraint satisfaction, stability, and lowest child death level from each child
		 * cluster to each parent cluster in the tree.  This method must be called before calling
		 * findProminentClusters() or calculateOutlierScores().
		 * @param clusters A list of Clusters forming a cluster tree
		 * @return true if there are any clusters with infinite stability, false otherwise
		 */
		public static bool PropagateTree(List<Cluster> clusters)
		{
			List<KeyValuePair<int, Cluster>> clustersToExamine = new List<KeyValuePair<int, Cluster>>();
			BitSet addedToExaminationList = new BitSet();
			bool infiniteStability = false;

			//Find all leaf clusters in the cluster tree:
			foreach (Cluster cluster in clusters)
			{
				if (cluster != null && !cluster.HasChildren())
				{
					KeyValuePair<int, Cluster>? item = clustersToExamine
						.Where(m => m.Key == cluster.GetLabel())
						.FirstOrDefault();
					if (item != null)
					{
						clustersToExamine.Remove(item.Value);
					}
					clustersToExamine.Add(new KeyValuePair<int, Cluster>(cluster.GetLabel(), cluster));
					addedToExaminationList.Set(cluster.GetLabel());
				}
			}

			//Iterate through every cluster, propagating stability from children to parents:
			while (clustersToExamine.Any())
			{
				var currentKeyValue = clustersToExamine
					.OrderBy(m => m.Key)
					.Last();
				Cluster currentCluster = currentKeyValue.Value;
				clustersToExamine.Remove(currentKeyValue);

				currentCluster.Propagate();

				if (currentCluster.GetStability() == Double.PositiveInfinity)
					infiniteStability = true;

				if (currentCluster.GetParent() != null)
				{
					Cluster parent = currentCluster.GetParent();

					if (!addedToExaminationList.Get(parent.GetLabel()))
					{
						KeyValuePair<int, Cluster>? item = clustersToExamine
							.Where(m => m.Key == parent.GetLabel())
							.FirstOrDefault();
						if (item != null)
						{
							clustersToExamine.Remove(item.Value);
						}
						clustersToExamine.Add(new KeyValuePair<int, Cluster>(parent.GetLabel(), parent));
						addedToExaminationList.Set(parent.GetLabel());
					}
				}
			}

			return infiniteStability;
		}

		/**
		 * Produces a flat clustering result using constraint satisfaction and cluster stability, and 
		 * returns an array of labels.  propagateTree() must be called before calling this method.
		 * @param clusters A list of Clusters forming a cluster tree which has already been propagated
		 * @param hierarchyFile The path to the hierarchy input file
		 * @param flatOutputFile The path to the flat clustering output file
		 * @param delimiter The delimiter for both files
		 * @param numPoints The number of points in the original data set
		 * @param infiniteStability true if there are any clusters with infinite stability, false otherwise
		 * @return An array of labels for the flat clustering result
		 * @throws IOException If any errors occur opening, reading, or writing to the files
		 * @throws NumberFormatException If illegal number values are found in the hierarchyFile
		 */
		public static int[] FindProminentClusters(
			List<Cluster> clusters,
			StringBuilder hierarchyWriter,
			char delimiter,
			int numPoints)
		{
			//Take the list of propagated clusters from the root cluster:
			List<Cluster> solution = clusters[1].GetPropagatedDescendants();

			var reader = hierarchyWriter.ToString();
			int[] flatPartitioning = new int[numPoints];

			//Store all the file offsets at which to find the birth points for the flat clustering:
			List<KeyValuePair<long, List<int>>> significantFileOffsets = new List<KeyValuePair<long, List<int>>>();

			foreach (Cluster cluster in solution)
			{
				List<int> clusterList = significantFileOffsets
					.Where(m => m.Key == cluster.GetFileOffset())
					.Select(m => m.Value)
					.FirstOrDefault();

				if (clusterList == null)
				{
					clusterList = new List<int>();

					KeyValuePair<long, List<int>>? item = significantFileOffsets
						.Where(m => m.Key == cluster.GetFileOffset())
						.FirstOrDefault();
					if (item != null)
					{
						significantFileOffsets.Remove(item.Value);
					}
					significantFileOffsets.Add(new KeyValuePair<long, List<int>>(cluster.GetFileOffset(), clusterList));
				}
				clusterList.Add(cluster.GetLabel());
			}

			//Go through the hierarchy file, setting labels for the flat clustering:
			while (significantFileOffsets.Any())
			{
				KeyValuePair<long, List<int>> entry = significantFileOffsets
						.OrderBy(m => m.Key)
						.FirstOrDefault();
				significantFileOffsets.Remove(entry);

				List<int> clusterList = entry.Value;
				long offset = entry.Key;

				int skip = (int)offset;
				int iSkip = skip;
				int length = 1;
				while (iSkip < reader.Length)
				{
					char c = reader[iSkip];
					if (c == '\n')
					{
						break;
					}
					iSkip++;
					length++;
				}
				
				string line = reader.Substring(skip, length);
				string[] lineContents = line.Split(delimiter);

				for (int i = 1; i < lineContents.Length; i++)
				{
					int label = int.Parse(lineContents[i]);
					if (clusterList.Contains(label))
						flatPartitioning[i - 1] = label;
				}
			}
			return flatPartitioning;
		}

		/**
		 * Produces the outlier score for each point in the data set, and returns a sorted list of outlier
		 * scores.  propagateTree() must be called before calling this method.
		 * @param clusters A list of Clusters forming a cluster tree which has already been propagated
		 * @param pointNoiseLevels A double[] with the levels at which each point became noise
		 * @param pointLastClusters An int[] with the last label each point had before becoming noise
		 * @param coreDistances An array of core distances for each data point
		 * @param outlierScoresOutputFile The path to the outlier scores output file
		 * @param delimiter The delimiter for the output file
		 * @param infiniteStability true if there are any clusters with infinite stability, false otherwise
		 * @return An List of OutlierScores, sorted in descending order
		 * @throws IOException If any errors occur opening or writing to the output file
		 */
		public static List<OutlierScore> CalculateOutlierScores(
			List<Cluster> clusters,
			double[] pointNoiseLevels,
			int[] pointLastClusters,
			double[] coreDistances)
		{
			int numPoints = pointNoiseLevels.Length;
			List<OutlierScore> outlierScores = new List<OutlierScore>(numPoints);

			//Iterate through each point, calculating its outlier score:
			for (int i = 0; i < numPoints; i++)
			{
				double epsilon_max = clusters[pointLastClusters[i]].GetPropagatedLowestChildDeathLevel();
				double epsilon = pointNoiseLevels[i];
				double score = 0;

				if (epsilon != 0)
					score = 1 - (epsilon_max / epsilon);

				outlierScores.Add(new OutlierScore(score, coreDistances[i], i));
			}

			//Sort the outlier scores:
			outlierScores.Sort();

			return outlierScores;
		}

		/**
		 * Removes the set of points from their parent Cluster, and creates a new Cluster, provided the
		 * clusterId is not 0 (noise).
		 * @param points The set of points to be in the new Cluster
		 * @param clusterLabels An array of cluster labels, which will be modified
		 * @param parentCluster The parent Cluster of the new Cluster being created
		 * @param clusterLabel The label of the new Cluster 
		 * @param edgeWeight The edge weight at which to remove the points from their previous Cluster
		 * @return The new Cluster, or null if the clusterId was 0
		 */
		private static Cluster CreateNewCluster(
			SortedSet<int> points,
			int[] clusterLabels,
			Cluster parentCluster,
			int clusterLabel,
			double edgeWeight)
		{
			foreach (int point in points)
			{
				clusterLabels[point] = clusterLabel;
			}

			parentCluster.DetachPoints(points.Count, edgeWeight);

			if (clusterLabel != 0)
				return new Cluster(clusterLabel, parentCluster, edgeWeight, points.Count);
			else
			{
				parentCluster.AddPointsToVirtualChildCluster(points);
				return null;
			}
		}

		/**
		 * Calculates the number of constraints satisfied by the new clusters and virtual children of the
		 * parents of the new clusters.
		 * @param newClusterLabels Labels of new clusters
		 * @param clusters An List of clusters
		 * @param constraints An List of constraints
		 * @param clusterLabels an array of current cluster labels for points
		 */
		private static void CalculateNumConstraintsSatisfied(
			SortedSet<int> newClusterLabels,
			List<Cluster> clusters,
			List<HdbscanConstraint> constraints,
			int[] clusterLabels)
		{
			if (constraints == null)
				return;

			List<Cluster> parents = new List<Cluster>();

			foreach (int label in newClusterLabels)
			{
				Cluster parent = clusters[label].GetParent();
				if (parent != null && !parents.Contains(parent))
					parents.Add(parent);
			}

			foreach (HdbscanConstraint constraint in constraints)
			{
				int labelA = clusterLabels[constraint.GetPointA()];
				int labelB = clusterLabels[constraint.GetPointB()];

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
						foreach (Cluster parent in parents)
						{
							if (parent.VirtualChildClusterContaintsPoint(constraint.GetPointA()))
							{
								parent.AddVirtualChildConstraintsSatisfied(1);
								break;
							}
						}
					}
					if (labelB == 0)
					{
						foreach (Cluster parent in parents)
						{
							if (parent.VirtualChildClusterContaintsPoint(constraint.GetPointB()))
							{
								parent.AddVirtualChildConstraintsSatisfied(1);
								break;
							}
						}
					}
				}
			}

			foreach (Cluster parent in parents)
			{
				parent.ReleaseVirtualChildCluster();
			}
		}
	}
}