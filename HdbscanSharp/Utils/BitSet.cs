using System;

namespace HdbscanSharp.Utils
{
	public class BitSet
	{
		private bool[] _bits = new bool[0];
		
		public bool Get(int pos)
		{
			return pos < _bits.Length && _bits[pos];
		}
		
		public void Set(int pos)
		{
			Ensure(pos);
			_bits[pos] = true;
		}
		
		private void Ensure(int pos)
		{
			if (pos >= _bits.Length)
			{
				var nd = new bool[pos + 64];
				Array.Copy(_bits, 0, nd, 0, _bits.Length);
				_bits = nd;
			}
		}
	}
}