using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Hdbscanstar
{
	/// <summary>
	/// An HDBSCAN* cluster, which will have a birth level, death level, stability, and constraint 
	/// satisfaction once fully constructed.
	/// </summary>
	public class Cluster
	{
		private readonly int _label;
		private readonly double _birthLevel;
		private double _deathLevel;
		private int _numPoints;
		private long _fileOffset;    //First level where points with this cluster's label appear
		private double _stability;
		private double _propagatedStability;
		private double _propagatedLowestChildDeathLevel;
		private int _numConstraintsSatisfied;
		private int _propagatedNumConstraintsSatisfied;
		private SortedSet<int> _virtualChildCluster;
		private readonly Cluster _parent;
		private bool _clusterHasChildren;
		private readonly List<Cluster> _propagatedDescendants;

		/// <summary>
		/// Creates a new Cluster.
		/// </summary>
		/// <param name="label">The cluster label, which should be globally unique</param>
		/// <param name="parent">The cluster which split to create this cluster</param>
		/// <param name="birthLevel">The MST edge level at which this cluster first appeared</param>
		/// <param name="numPoints">The initial number of points in this cluster</param>
		public Cluster(int label, Cluster parent, double birthLevel, int numPoints)
		{
			_label = label;
			_birthLevel = birthLevel;
			_deathLevel = 0;
			_numPoints = numPoints;
			_fileOffset = 0;
			_stability = 0;
			_propagatedStability = 0;
			_propagatedLowestChildDeathLevel = double.MaxValue;
			_numConstraintsSatisfied = 0;
			_propagatedNumConstraintsSatisfied = 0;
			_virtualChildCluster = new SortedSet<int>();
			_parent = parent;
			if (_parent != null)
				_parent._clusterHasChildren = true;
			_clusterHasChildren = false;
			_propagatedDescendants = new List<Cluster>(1);
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
			_stability += (numPoints * (1 / level - 1 / _birthLevel));

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
			if (_parent != null)
			{
				//Propagate lowest death level of any descendants:
				if (_propagatedLowestChildDeathLevel == double.MaxValue)
					_propagatedLowestChildDeathLevel = _deathLevel;
				if (_propagatedLowestChildDeathLevel < _parent._propagatedLowestChildDeathLevel)
					_parent._propagatedLowestChildDeathLevel = _propagatedLowestChildDeathLevel;
				
				//If this cluster has no children, it must propagate itself:
				if (!_clusterHasChildren)
				{
					_parent._propagatedNumConstraintsSatisfied += _numConstraintsSatisfied;
					_parent._propagatedStability += _stability;
					_parent._propagatedDescendants.Add(this);
				}
				else if (_numConstraintsSatisfied > _propagatedNumConstraintsSatisfied)
				{
					_parent._propagatedNumConstraintsSatisfied += _numConstraintsSatisfied;
					_parent._propagatedStability += _stability;
					_parent._propagatedDescendants.Add(this);
				}
				else if (_numConstraintsSatisfied < _propagatedNumConstraintsSatisfied)
				{
					_parent._propagatedNumConstraintsSatisfied += _propagatedNumConstraintsSatisfied;
					_parent._propagatedStability += _propagatedStability;
					_parent._propagatedDescendants.AddRange(_propagatedDescendants);
				}
				else if (_numConstraintsSatisfied == _propagatedNumConstraintsSatisfied)
				{
					//Chose the parent over descendants if there is a tie in stability:
					if (_stability >= _propagatedStability)
					{
						_parent._propagatedNumConstraintsSatisfied += _numConstraintsSatisfied;
						_parent._propagatedStability += _stability;
						_parent._propagatedDescendants.Add(this);
					}
					else
					{
						_parent._propagatedNumConstraintsSatisfied += _propagatedNumConstraintsSatisfied;
						_parent._propagatedStability += _propagatedStability;
						_parent._propagatedDescendants.AddRange(_propagatedDescendants);
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

		public int GetLabel()
		{
			return _label;
		}

		public Cluster GetParent()
		{
			return _parent;
		}

		public long GetFileOffset()
		{
			return _fileOffset;
		}

		public void SetFileOffset(long offset)
		{
			_fileOffset = offset;
		}

		public double GetStability()
		{
			return _stability;
		}

		public double GetPropagatedLowestChildDeathLevel()
		{
			return _propagatedLowestChildDeathLevel;
		}

		public IEnumerable<Cluster> GetPropagatedDescendants()
		{
			return _propagatedDescendants;
		}

		public bool HasChildren()
		{
			return _clusterHasChildren;
		}
	}
}
