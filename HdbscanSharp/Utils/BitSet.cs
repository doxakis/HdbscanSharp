using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Utils
{
	public class BitSet
	{
		private bool[] bits = new bool[0];
		
		public bool Get(int pos)
		{
			if (pos >= bits.Length)
			{
				return false;
			}
			return bits[pos];
		}
		
		public void Set(int pos)
		{
			Ensure(pos);
			bits[pos] = true;
		}
		
		private void Ensure(int pos)
		{
			if (pos >= bits.Length)
			{
				bool[] nd = new bool[pos + 64];
				Array.Copy(bits, 0, nd, 0, bits.Length);
				bits = nd;
			}
		}
	}
}