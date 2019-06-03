namespace HdbscanSharp.Hdbscanstar
{
	/// <summary>
	/// A clustering constraint (either a must-link or cannot-link constraint between two points).
	/// </summary>
	public class HdbscanConstraint
	{
		private readonly HdbscanConstraintType _constraintType;
		private readonly int _pointA;
		private readonly int _pointB;

		/// <summary>
		/// Creates a new constraint.
		/// </summary>
		/// <param name="pointA">The first point involved in the constraint</param>
		/// <param name="pointB">The second point involved in the constraint</param>
		/// <param name="type">The constraint type</param>
		public HdbscanConstraint(int pointA, int pointB, HdbscanConstraintType type)
		{
			_pointA = pointA;
			_pointB = pointB;
			_constraintType = type;
		}

		public int GetPointA()
		{
			return _pointA;
		}

		public int GetPointB()
		{
			return _pointB;
		}
		
		public HdbscanConstraintType GetConstraintType()
		{
			return _constraintType;
		}
	}
}
