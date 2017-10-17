using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdbscanSharp.Utils
{
	public class BitSet
	{
		private bool[] Bits = new bool[0];
		
		public bool Get(int pos)
		{
			if (pos >= Bits.Length)
			{
				return false;
			}
			return Bits[pos];
		}
		
		public void Set(int pos)
		{
			Ensure(pos);
			Bits[pos] = true;
		}
		
		private void Ensure(int pos)
		{
			if (pos >= Bits.Length)
			{
				bool[] nd = new bool[pos + 64];
				Array.Copy(Bits, 0, nd, 0, Bits.Length);
				Bits = nd;
			}
		}
	}
}