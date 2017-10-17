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
		private int label;
		private double birthLevel;
		private double deathLevel;
		private int numPoints;
		private long fileOffset;    //First level where points with this cluster's label appear
		private double stability;
		private double propagatedStability;
		private double propagatedLowestChildDeathLevel;
		private int numConstraintsSatisfied;
		private int propagatedNumConstraintsSatisfied;
		private SortedSet<int> virtualChildCluster;
		private Cluster parent;
		private bool hasChildren;
		public List<Cluster> propagatedDescendants;

		/// <summary>
		/// Creates a new Cluster.
		/// </summary>
		/// <param name="label">The cluster label, which should be globally unique</param>
		/// <param name="parent">The cluster which split to create this cluster</param>
		/// <param name="birthLevel">The MST edge level at which this cluster first appeared</param>
		/// <param name="numPoints">The initial number of points in this cluster</param>
		public Cluster(int label, Cluster parent, double birthLevel, int numPoints)
		{
			this.label = label;
			this.birthLevel = birthLevel;
			this.deathLevel = 0;
			this.numPoints = numPoints;
			this.fileOffset = 0;
			this.stability = 0;
			this.propagatedStability = 0;
			this.propagatedLowestChildDeathLevel = Double.MaxValue;
			this.numConstraintsSatisfied = 0;
			this.propagatedNumConstraintsSatisfied = 0;
			this.virtualChildCluster = new SortedSet<int>();
			this.parent = parent;
			if (this.parent != null)
				this.parent.hasChildren = true;
			this.hasChildren = false;
			this.propagatedDescendants = new List<Cluster>(1);
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
			this.numPoints -= numPoints;
			this.stability += (numPoints * (1 / level - 1 / this.birthLevel));

			if (this.numPoints == 0)
				this.deathLevel = level;
			else if (this.numPoints < 0)
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
			if (this.parent != null)
			{
				//Propagate lowest death level of any descendants:
				if (this.propagatedLowestChildDeathLevel == Double.MaxValue)
					this.propagatedLowestChildDeathLevel = this.deathLevel;
				if (this.propagatedLowestChildDeathLevel < this.parent.propagatedLowestChildDeathLevel)
					this.parent.propagatedLowestChildDeathLevel = this.propagatedLowestChildDeathLevel;
				
				//If this cluster has no children, it must propagate itself:
				if (!this.hasChildren)
				{
					this.parent.propagatedNumConstraintsSatisfied += this.numConstraintsSatisfied;
					this.parent.propagatedStability += this.stability;
					this.parent.propagatedDescendants.Add(this);
				}
				else if (this.numConstraintsSatisfied > this.propagatedNumConstraintsSatisfied)
				{
					this.parent.propagatedNumConstraintsSatisfied += this.numConstraintsSatisfied;
					this.parent.propagatedStability += this.stability;
					this.parent.propagatedDescendants.Add(this);
				}
				else if (this.numConstraintsSatisfied < this.propagatedNumConstraintsSatisfied)
				{
					this.parent.propagatedNumConstraintsSatisfied += this.propagatedNumConstraintsSatisfied;
					this.parent.propagatedStability += this.propagatedStability;
					this.parent.propagatedDescendants.AddRange(this.propagatedDescendants);
				}
				else if (this.numConstraintsSatisfied == this.propagatedNumConstraintsSatisfied)
				{
					//Chose the parent over descendants if there is a tie in stability:
					if (this.stability >= this.propagatedStability)
					{
						this.parent.propagatedNumConstraintsSatisfied += this.numConstraintsSatisfied;
						this.parent.propagatedStability += this.stability;
						this.parent.propagatedDescendants.Add(this);
					}
					else
					{
						this.parent.propagatedNumConstraintsSatisfied += this.propagatedNumConstraintsSatisfied;
						this.parent.propagatedStability += this.propagatedStability;
						this.parent.propagatedDescendants.AddRange(this.propagatedDescendants);
					}
				}
			}
		}

		public void AddPointsToVirtualChildCluster(SortedSet<int> points)
		{
			foreach (var point in points)
			{
				this.virtualChildCluster.Add(point);
			}
		}

		public bool VirtualChildClusterContaintsPoint(int point)
		{
			return this.virtualChildCluster.Contains(point);
		}

		public void AddVirtualChildConstraintsSatisfied(int numConstraints)
		{
			this.propagatedNumConstraintsSatisfied += numConstraints;
		}

		public void AddConstraintsSatisfied(int numConstraints)
		{
			this.numConstraintsSatisfied += numConstraints;
		}

		/// <summary>
		/// Sets the virtual child cluster to null, thereby saving memory.  Only call this method after computing the
		/// number of constraints satisfied by the virtual child cluster.
		/// </summary>
		public void ReleaseVirtualChildCluster()
		{
			this.virtualChildCluster = null;
		}

		public int GetLabel()
		{
			return this.label;
		}

		public Cluster GetParent()
		{
			return this.parent;
		}

		public double GetBirthLevel()
		{
			return this.birthLevel;
		}

		public double GetDeathLevel()
		{
			return this.deathLevel;
		}

		public long GetFileOffset()
		{
			return this.fileOffset;
		}

		public void SetFileOffset(long offset)
		{
			this.fileOffset = offset;
		}

		public double GetStability()
		{
			return this.stability;
		}

		public double GetPropagatedLowestChildDeathLevel()
		{
			return this.propagatedLowestChildDeathLevel;
		}

		public int GetNumConstraintsSatisfied()
		{
			return this.numConstraintsSatisfied;
		}

		public int GetPropagatedNumConstraintsSatisfied()
		{
			return this.propagatedNumConstraintsSatisfied;
		}

		public List<Cluster> GetPropagatedDescendants()
		{
			return this.propagatedDescendants;
		}

		public bool HasChildren()
		{
			return this.hasChildren;
		}
	}
}
