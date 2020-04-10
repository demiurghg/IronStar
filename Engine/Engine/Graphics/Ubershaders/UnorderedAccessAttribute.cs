using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Ubershaders {

	internal abstract class UnorderedAccessAttribute : Attribute 
	{
		public uint Group = 0xFFFFFFFF;
		public int InitCount = -1;
		public abstract string GetBindingName ();
	}


	internal sealed class RWStructuredBufferAttribute : UnorderedAccessAttribute 
	{
		readonly Type type;

		public RWStructuredBufferAttribute (Type type, int initCount)
		{
			this.type		=	type;
			this.InitCount	=	initCount;
		}

		public override string GetBindingName()
		{
			return "RWStructuredBuffer<" + UbershaderGenerator.GetStructFieldHLSLType(type, true) + ">";
		}
	}


	internal sealed class RWBufferAttribute : UnorderedAccessAttribute 
	{
		readonly Type type;

		public RWBufferAttribute (Type type)
		{
			this.type	=	type;
		}

		public override string GetBindingName()
		{
			return "RWBuffer<" + UbershaderGenerator.GetStructFieldHLSLType(type, false) + ">";
		}
	}


	internal sealed class RWByteAddressBufferAttribute : UnorderedAccessAttribute 
	{
		readonly Type type;

		public RWByteAddressBufferAttribute ()
		{
		}

		public override string GetBindingName()
		{
			return "RWByteAddressBuffer";
		}
	}


	internal sealed class RWTexture2DAttribute : UnorderedAccessAttribute 
	{
		readonly Type type;

		public RWTexture2DAttribute (Type type)
		{
			this.type	=	type;
		}

		public override string GetBindingName()
		{
			return "RWTexture2D<" + UbershaderGenerator.GetStructFieldHLSLType(type, false) + ">";
		}
	}


	internal sealed class RWTexture2DArrayAttribute : UnorderedAccessAttribute 
	{
		readonly Type type;

		public RWTexture2DArrayAttribute (Type type)
		{
			this.type	=	type;
		}

		public override string GetBindingName()
		{
			return "RWTexture2DArray<" + UbershaderGenerator.GetStructFieldHLSLType(type, false) + ">";
		}
	}


	internal sealed class RWTexture3DAttribute : UnorderedAccessAttribute 
	{
		readonly Type type;

		public RWTexture3DAttribute (Type type)
		{
			this.type	=	type;
		}

		public override string GetBindingName()
		{
			return "RWTexture3D<" + UbershaderGenerator.GetStructFieldHLSLType(type, false) + ">";
		}
	}
}
