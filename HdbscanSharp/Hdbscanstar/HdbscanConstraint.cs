using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Hdbscanstar
{
	/**
	 * A clustering constraint (either a must-link or cannot-link constraint between two points).
	 */
	public class HdbscanConstraint
	{
		private HdbscanConstraintType type;
		private int pointA;
		private int pointB;

		/**
		 * Creates a new constraint.
		 * @param pointA The first point involved in the constraint
		 * @param pointB The second point involved in the constraint
		 * @param type The CONSTRAINT_TYPE of the constraint
		 */
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
