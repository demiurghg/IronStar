using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Engine.Graphics {

	/// <summary>
	/// Represents the address of virtual texture tile.
	/// Address consist of page X index, page Y index & mip level.
	/// Non-zero dummy used for GPU readback. Zero dummy is ignored.
	/// </summary>
	public struct VTAddress : IEquatable<VTAddress> 
	{
		uint rawAddr;

		public int PageX	{ get { return (int)((rawAddr >>  0) & 0xFFF ); } }
		public int PageY	{ get { return (int)((rawAddr >> 12) & 0xFFF ); } }
		public int MipLevel	{ get { return (int)((rawAddr >> 24) & 0x7   ); } }
		public bool IsBad	{ get { return (rawAddr & 0xF0000000)!=0; }	}


		public static VTAddress CreateBadAddress (int uniqueNumber) 
		{
			return new VTAddress( unchecked(0xF0000000 | (uint)uniqueNumber));
		}


		private VTAddress( uint raw )
		{
			rawAddr	=	raw;
		}


		public VTAddress ( int pageX, int pageY, int mipLevel )
		{
			if (pageX>=VTConfig.TextureSize) {
				throw new ArgumentException("pageX");
			}
			if (pageY>=VTConfig.TextureSize) {
				throw new ArgumentException("pageY");
			}
			if (mipLevel>VTConfig.MaxMipLevel) {
				throw new ArgumentException("mipLevel");
			}

			/*var pageX		= (uint)(pageX & (VTConfig.TextureSize-1));
			var pageY		= (uint)(pageY & (VTConfig.TextureSize-1));
			var mipLevel	= (uint)(mipLevel & 0x7);*/

			rawAddr			= unchecked(((uint)mipLevel << 24) | ((uint)pageY << 12) | (uint)pageX);
		}



		public static bool CheckBadAddress ( int pageX, int pageY, int mipLevel, bool showWarning )
		{
			if (mipLevel<0 | mipLevel>=VTConfig.MipCount) {
				if (showWarning) {
					Log.Warning("Bad mip level: {0} - [0..{1})", mipLevel, VTConfig.MipCount);
				}
				return false;
			}

			int maxPageCount = VTConfig.VirtualPageCount >> mipLevel;

			if (pageX<0 | pageX>=maxPageCount) {
				if (showWarning) {
					Log.Warning("Bad page X: {0} - [0..{1})", pageX, maxPageCount);
				}
				return false;
			}
			if (pageY<0 | pageY>=maxPageCount) {
				if (showWarning) {
					Log.Warning("Bad page Y: {0} - [0..{1})", pageY, maxPageCount);
				}
				return false;
			}

			return true;
		}



		public static VTAddress FromChild ( VTAddress feedback )
		{
			if (feedback.MipLevel >= VTConfig.MaxMipLevel) {
				throw new ArgumentException("mip >= max mip");
			}

			var pageX		= feedback.PageX/2;
			var pageY		= feedback.PageY/2;
			var mipLevel	= feedback.MipLevel + 1;

			return new VTAddress( pageX, pageY, mipLevel );
		}


		public bool IsLeastDetailed 
		{
			get { return (MipLevel == VTConfig.MaxMipLevel); }
		}


		public VTAddress GetLessDetailedMip ()
		{
			return FromChild(this);
		}



		public override string ToString ()
		{
			return string.Format("{0},{1}:{2}", PageX, PageY, MipLevel );
		}


		private bool Equals(ref VTAddress other)
		{
			return	rawAddr==other.rawAddr;
		}


		public bool Equals(VTAddress other)
		{
			return Equals(ref other);
		}


		public override bool Equals(object value)
		{
			if (!(value is VTAddress))
				return false;

			var strongValue = (VTAddress)value;
			return Equals(ref strongValue);
		}


		public override int GetHashCode ()
		{
			return unchecked((int)rawAddr);
		}


		public UInt32 ComputeUIntAddress () { return unchecked((uint)rawAddr); }

		public Int32 ComputeIntAddress () {	return unchecked((int)rawAddr);	}

		public string GetFileNameWithoutExtension (string postfix)
		{
			return ComputeUIntAddress().ToString("X8") + postfix;
		}
	}

}
