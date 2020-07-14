using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	public struct BitSet
	{
		public const uint MaxBits = 32;

		uint bits;


		public BitSet( uint bits )
		{
			this.bits = bits;
		}


		uint GetBit(int index)
		{
			if (index<0 || index>=MaxBits) 
			{
				throw new ArgumentOutOfRangeException(string.Format("Index ({0}) must be greater or equal zero and less than {1}", index, MaxBits ));
			}

			return 1u << index;
		}


		public bool this[int index]
		{
			get 
			{
				return (bits & GetBit(index))!=0;
			}

			set 
			{
				bits = value ? (bits | GetBit(index)) : (bits & ~GetBit(index));
			}
		}


		public void Clear()
		{
			bits = 0;
		}


		public override string ToString()
		{
			return bits.ToString();
		}


		public override int GetHashCode()
		{
			unchecked
			{
				return bits.GetHashCode();
			}
		}


		public bool Equals(BitSet other)
		{
			return other.bits == bits;
		}


		public override bool Equals(object value)
		{
			if (value == null)
				return false;

			if (!ReferenceEquals(value.GetType(), typeof(BitSet)))
				return false;

			return Equals((BitSet) value);
		}
	}
}
