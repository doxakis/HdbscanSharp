using System;
using System.Collections.Generic;

namespace HdbscanSharp.Hdbscanstar
{
	/// <summary>
	/// An HDBSCAN* cluster, which will have a birth level, death level, stability, and constraint 
	/// satisfaction once fully constructed.
	/// </summary>
	public class Cluster
	{
		private readonly double _birthLevel;
		private double _deathLevel;
		private int _numPoints;
		private double _propagatedStability;
		private int _numConstraintsSatisfied;
		private int _propagatedNumConstraintsSatisfied;
		private SortedSet<int> _virtualChildCluster;

		public List<Cluster> PropagatedDescendants { get; }
		public double PropagatedLowestChildDeathLevel { get; internal set; }
		public Cluster Parent { get; }
		public double Stability { get; internal set; }
		public bool HasChildren { get; internal set; }
		public int Label { get; }
		public int HierarchyPosition { get; set; }    //First level where points with this cluster's label appear

		/// <summary>
		/// Creates a new Cluster.
		/// </summary>
		/// <param name="label">The cluster label, which should be globally unique</param>
		/// <param name="parent">The cluster which split to create this cluster</param>
		/// <param name="birthLevel">The MST edge level at which this cluster first appeared</param>
		/// <param name="numPoints">The initial number of points in this cluster</param>
		public Cluster(int label, Cluster parent, double birthLevel, int numPoints)
		{
			_birthLevel = birthLevel;
			_deathLevel = 0;
			_numPoints = numPoints;
			_propagatedStability = 0;
			_numConstraintsSatisfied = 0;
			_propagatedNumConstraintsSatisfied = 0;
			_virtualChildCluster = new SortedSet<int>();

			Label = label;
			HierarchyPosition = 0;
			Stability = 0;
			PropagatedLowestChildDeathLevel = double.MaxValue;
			Parent = parent;
			if (Parent != null)
				Parent.HasChildren = true;
			HasChildren = false;
			PropagatedDescendants = new List<Cluster>(1);
		}

		/// <summary>
		/// Removes the specified number of points from this cluster at the given edge level, which will
		/// update the stability of this cluster and potentially cause cluster death.  If cluster death
		/// occurs, the number of constraints satisfied by the virtual child cluster will also be calculated.
		/// </summary>
		/// <param name="numPoints">The number of points to remove from the cluster</param>
		/// <param name="level">The MST edge level at which to remove these points</param>
		public void DetachPoints(int numPoints, double level)
		{
			_numPoints -= numPoints;
			Stability += (numPoints * (1 / level - 1 / _birthLevel));

			if (_numPoints == 0)
				_deathLevel = level;
			else if (_numPoints < 0)
				throw new InvalidOperationException("Cluster cannot have less than 0 points.");
		}

		/// <summary>
		/// This cluster will propagate itself to its parent if its number of satisfied constraints is
		/// higher than the number of propagated constraints.  Otherwise, this cluster propagates its
		/// propagated descendants.  In the case of ties, stability is examined.
		/// Additionally, this cluster propagates the lowest death level of any of its descendants to its
		/// parent.
		/// </summary>
		public void Propagate()
		{
			if (Parent != null)
			{
				//Propagate lowest death level of any descendants:
				if (PropagatedLowestChildDeathLevel == double.MaxValue)
					PropagatedLowestChildDeathLevel = _deathLevel;
				if (PropagatedLowestChildDeathLevel < Parent.PropagatedLowestChildDeathLevel)
					Parent.PropagatedLowestChildDeathLevel = PropagatedLowestChildDeathLevel;
				
				//If this cluster has no children, it must propagate itself:
				if (!HasChildren)
				{
					Parent._propagatedNumConstraintsSatisfied += _numConstraintsSatisfied;
					Parent._propagatedStability += Stability;
					Parent.PropagatedDescendants.Add(this);
				}
				else if (_numConstraintsSatisfied > _propagatedNumConstraintsSatisfied)
				{
					Parent._propagatedNumConstraintsSatisfied += _numConstraintsSatisfied;
					Parent._propagatedStability += Stability;
					Parent.PropagatedDescendants.Add(this);
				}
				else if (_numConstraintsSatisfied < _propagatedNumConstraintsSatisfied)
				{
					Parent._propagatedNumConstraintsSatisfied += _propagatedNumConstraintsSatisfied;
					Parent._propagatedStability += _propagatedStability;
					Parent.PropagatedDescendants.AddRange(PropagatedDescendants);
				}
				else if (_numConstraintsSatisfied == _propagatedNumConstraintsSatisfied)
				{
					//Chose the parent over descendants if there is a tie in stability:
					if (Stability >= _propagatedStability)
					{
						Parent._propagatedNumConstraintsSatisfied += _numConstraintsSatisfied;
						Parent._propagatedStability += Stability;
						Parent.PropagatedDescendants.Add(this);
					}
					else
					{
						Parent._propagatedNumConstraintsSatisfied += _propagatedNumConstraintsSatisfied;
						Parent._propagatedStability += _propagatedStability;
						Parent.PropagatedDescendants.AddRange(PropagatedDescendants);
					}
				}
			}
		}

		public void AddPointsToVirtualChildCluster(SortedSet<int> points)
		{
			foreach (var point in points)
			{
				_virtualChildCluster.Add(point);
			}
		}

		public bool VirtualChildClusterConstraintsPoint(int point)
		{
			return _virtualChildCluster.Contains(point);
		}

		public void AddVirtualChildConstraintsSatisfied(int numConstraints)
		{
			_propagatedNumConstraintsSatisfied += numConstraints;
		}

		public void AddConstraintsSatisfied(int numConstraints)
		{
			_numConstraintsSatisfied += numConstraints;
		}

		/// <summary>
		/// Sets the virtual child cluster to null, thereby saving memory.  Only call this method after computing the
		/// number of constraints satisfied by the virtual child cluster.
		/// </summary>
		public void ReleaseVirtualChildCluster()
		{
			_virtualChildCluster = null;
		}
	}
}
