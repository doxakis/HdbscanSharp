using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Hdbscanstar
{
	/// <summary>
	/// A clustering constraint (either a must-link or cannot-link constraint between two points).
	/// </summary>
	public class HdbscanConstraint
	{
		private HdbscanConstraintType ConstraintType;
		private int PointA;
		private int PointB;

		/// <summary>
		/// Creates a new constraint.
		/// </summary>
		/// <param name="pointA">The first point involved in the constraint</param>
		/// <param name="pointB">The second point involved in the constraint</param>
		/// <param name="type">The constraint type</param>
		public HdbscanConstraint(int pointA, int pointB, HdbscanConstraintType type)
		{
			this.PointA = pointA;
			this.PointB = pointB;
			this.ConstraintType = type;
		}

		public int GetPointA()
		{
			return this.PointA;
		}

		public int GetPointB()
		{
			return this.PointB;
		}
		
		public HdbscanConstraintType GetConstraintType()
		{
			return this.ConstraintType;
		}
	}
}
