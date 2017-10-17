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
		private HdbscanConstraintType type;
		private int pointA;
		private int pointB;

		/// <summary>
		/// Creates a new constraint.
		/// </summary>
		/// <param name="pointA">The first point involved in the constraint</param>
		/// <param name="pointB">The second point involved in the constraint</param>
		/// <param name="type">The constraint type</param>
		public HdbscanConstraint(int pointA, int pointB, HdbscanConstraintType type)
		{
			this.pointA = pointA;
			this.pointB = pointB;
			this.type = type;
		}

		public int GetPointA()
		{
			return this.pointA;
		}

		public int GetPointB()
		{
			return this.pointB;
		}
		
		public HdbscanConstraintType GetConstraintType()
		{
			return this.type;
		}
	}
}
