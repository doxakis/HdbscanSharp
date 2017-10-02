using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDBSCAN.Hdbscanstar
{
	/**
	 * A clustering constraint (either a must-link or cannot-link constraint between two points).
	 */
	public class Constraint
	{
		private CONSTRAINT_TYPE type;
		private int pointA;
		private int pointB;

		public const string MUST_LINK_TAG = "ml";
		public const string CANNOT_LINK_TAG = "cl";

		public enum CONSTRAINT_TYPE
		{
			NONE,
			MUST_LINK,
			CANNOT_LINK
		}

		/**
		 * Creates a new constraint.
		 * @param pointA The first point involved in the constraint
		 * @param pointB The second point involved in the constraint
		 * @param type The CONSTRAINT_TYPE of the constraint
		 */
		public Constraint(int pointA, int pointB, CONSTRAINT_TYPE type)
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
		
		public CONSTRAINT_TYPE GetConstraintType()
		{
			return this.type;
		}
	}
}
