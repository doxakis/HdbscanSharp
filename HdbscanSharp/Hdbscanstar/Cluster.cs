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
		private int Label;
		private double BirthLevel;
		private double DeathLevel;
		private int NumPoints;
		private long FileOffset;    //First level where points with this cluster's label appear
		private double Stability;
		private double PropagatedStability;
		private double PropagatedLowestChildDeathLevel;
		private int NumConstraintsSatisfied;
		private int PropagatedNumConstraintsSatisfied;
		private SortedSet<int> VirtualChildCluster;
		private Cluster Parent;
		private bool ClusterHasChildren;
		public List<Cluster> PropagatedDescendants;

		/// <summary>
		/// Creates a new Cluster.
		/// </summary>
		/// <param name="label">The cluster label, which should be globally unique</param>
		/// <param name="parent">The cluster which split to create this cluster</param>
		/// <param name="birthLevel">The MST edge level at which this cluster first appeared</param>
		/// <param name="numPoints">The initial number of points in this cluster</param>
		public Cluster(int label, Cluster parent, double birthLevel, int numPoints)
		{
			this.Label = label;
			this.BirthLevel = birthLevel;
			this.DeathLevel = 0;
			this.NumPoints = numPoints;
			this.FileOffset = 0;
			this.Stability = 0;
			this.PropagatedStability = 0;
			this.PropagatedLowestChildDeathLevel = Double.MaxValue;
			this.NumConstraintsSatisfied = 0;
			this.PropagatedNumConstraintsSatisfied = 0;
			this.VirtualChildCluster = new SortedSet<int>();
			this.Parent = parent;
			if (this.Parent != null)
				this.Parent.ClusterHasChildren = true;
			this.ClusterHasChildren = false;
			this.PropagatedDescendants = new List<Cluster>(1);
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
			this.NumPoints -= numPoints;
			this.Stability += (numPoints * (1 / level - 1 / this.BirthLevel));

			if (this.NumPoints == 0)
				this.DeathLevel = level;
			else if (this.NumPoints < 0)
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
			if (this.Parent != null)
			{
				//Propagate lowest death level of any descendants:
				if (this.PropagatedLowestChildDeathLevel == Double.MaxValue)
					this.PropagatedLowestChildDeathLevel = this.DeathLevel;
				if (this.PropagatedLowestChildDeathLevel < this.Parent.PropagatedLowestChildDeathLevel)
					this.Parent.PropagatedLowestChildDeathLevel = this.PropagatedLowestChildDeathLevel;
				
				//If this cluster has no children, it must propagate itself:
				if (!this.ClusterHasChildren)
				{
					this.Parent.PropagatedNumConstraintsSatisfied += this.NumConstraintsSatisfied;
					this.Parent.PropagatedStability += this.Stability;
					this.Parent.PropagatedDescendants.Add(this);
				}
				else if (this.NumConstraintsSatisfied > this.PropagatedNumConstraintsSatisfied)
				{
					this.Parent.PropagatedNumConstraintsSatisfied += this.NumConstraintsSatisfied;
					this.Parent.PropagatedStability += this.Stability;
					this.Parent.PropagatedDescendants.Add(this);
				}
				else if (this.NumConstraintsSatisfied < this.PropagatedNumConstraintsSatisfied)
				{
					this.Parent.PropagatedNumConstraintsSatisfied += this.PropagatedNumConstraintsSatisfied;
					this.Parent.PropagatedStability += this.PropagatedStability;
					this.Parent.PropagatedDescendants.AddRange(this.PropagatedDescendants);
				}
				else if (this.NumConstraintsSatisfied == this.PropagatedNumConstraintsSatisfied)
				{
					//Chose the parent over descendants if there is a tie in stability:
					if (this.Stability >= this.PropagatedStability)
					{
						this.Parent.PropagatedNumConstraintsSatisfied += this.NumConstraintsSatisfied;
						this.Parent.PropagatedStability += this.Stability;
						this.Parent.PropagatedDescendants.Add(this);
					}
					else
					{
						this.Parent.PropagatedNumConstraintsSatisfied += this.PropagatedNumConstraintsSatisfied;
						this.Parent.PropagatedStability += this.PropagatedStability;
						this.Parent.PropagatedDescendants.AddRange(this.PropagatedDescendants);
					}
				}
			}
		}

		public void AddPointsToVirtualChildCluster(SortedSet<int> points)
		{
			foreach (var point in points)
			{
				this.VirtualChildCluster.Add(point);
			}
		}

		public bool VirtualChildClusterContaintsPoint(int point)
		{
			return this.VirtualChildCluster.Contains(point);
		}

		public void AddVirtualChildConstraintsSatisfied(int numConstraints)
		{
			this.PropagatedNumConstraintsSatisfied += numConstraints;
		}

		public void AddConstraintsSatisfied(int numConstraints)
		{
			this.NumConstraintsSatisfied += numConstraints;
		}

		/// <summary>
		/// Sets the virtual child cluster to null, thereby saving memory.  Only call this method after computing the
		/// number of constraints satisfied by the virtual child cluster.
		/// </summary>
		public void ReleaseVirtualChildCluster()
		{
			this.VirtualChildCluster = null;
		}

		public int GetLabel()
		{
			return this.Label;
		}

		public Cluster GetParent()
		{
			return this.Parent;
		}

		public double GetBirthLevel()
		{
			return this.BirthLevel;
		}

		public double GetDeathLevel()
		{
			return this.DeathLevel;
		}

		public long GetFileOffset()
		{
			return this.FileOffset;
		}

		public void SetFileOffset(long offset)
		{
			this.FileOffset = offset;
		}

		public double GetStability()
		{
			return this.Stability;
		}

		public double GetPropagatedLowestChildDeathLevel()
		{
			return this.PropagatedLowestChildDeathLevel;
		}

		public int GetNumConstraintsSatisfied()
		{
			return this.NumConstraintsSatisfied;
		}

		public int GetPropagatedNumConstraintsSatisfied()
		{
			return this.PropagatedNumConstraintsSatisfied;
		}

		public List<Cluster> GetPropagatedDescendants()
		{
			return this.PropagatedDescendants;
		}

		public bool HasChildren()
		{
			return this.ClusterHasChildren;
		}
	}
}
