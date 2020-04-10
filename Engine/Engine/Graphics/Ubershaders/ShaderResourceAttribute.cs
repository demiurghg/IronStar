using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Ubershaders {

	internal abstract class ShaderResourceAttribute : Attribute 
	{
		public uint Group = 0xFFFFFFFF;
		public string IfDef = null;
		public abstract string GetBindingName ();
	}


	internal sealed class ShaderStructuredBufferAttribute : ShaderResourceAttribute 
	{
		readonly Type type;

		public ShaderStructuredBufferAttribute (Type type)
		{
			this.type	=	type;
		}

		public override string GetBindingName()
		{
			return "StructuredBuffer<" + UbershaderGenerator.GetStructFieldHLSLType(type, true) + ">";
		}
	}


	internal sealed class ShaderBufferAttribute : ShaderResourceAttribute 
	{
		readonly Type type;

		public ShaderBufferAttribute (Type type)
		{
			this.type	=	type;
		}

		public override string GetBindingName()
		{
			return "Buffer<" + UbershaderGenerator.GetStructFieldHLSLType(type, false) + ">";
		}
	}


	internal sealed class ShaderTexture2DAttribute : ShaderResourceAttribute 
	{
		readonly Type type;

		public ShaderTexture2DAttribute (Type type=null)
		{
			this.type	=	type ?? typeof(Vector4);
		}

		public override string GetBindingName()
		{
			return "Texture2D<" + UbershaderGenerator.GetStructFieldHLSLType(type, false) + ">";
		}
	}


	internal sealed class ShaderTexture2DArrayAttribute : ShaderResourceAttribute 
	{
		readonly Type type;

		public ShaderTexture2DArrayAttribute (Type type=null)
		{
			this.type	=	type ?? typeof(Vector4);
		}

		public override string GetBindingName()
		{
			return "Texture2DArray<" + UbershaderGenerator.GetStructFieldHLSLType(type, false) + ">";
		}
	}


	internal sealed class ShaderTextureCubeAttribute : ShaderResourceAttribute 
	{
		readonly Type type;

		public ShaderTextureCubeAttribute (string texType, Type type=null)
		{
			this.type	=	type ?? typeof(Vector4);
		}

		public override string GetBindingName()
		{
			return "TextureCube" + ((type==null) ? "" : UbershaderGenerator.GetStructFieldHLSLType(type, false));
		}
	}


	internal sealed class ShaderTextureCubeArrayAttribute : ShaderResourceAttribute 
	{
		readonly Type type;

		public ShaderTextureCubeArrayAttribute (Type type=null)
		{
			this.type	=	type ?? typeof(Vector4);
		}

		public override string GetBindingName()
		{
			return "TextureCubeArray<" + UbershaderGenerator.GetStructFieldHLSLType(type, false) + ">";
		}
	}


	internal sealed class ShaderTexture3DAttribute : ShaderResourceAttribute 
	{
		readonly Type type;

		public ShaderTexture3DAttribute (Type type=null)
		{
			this.type	=	type ?? typeof(Vector4);
		}

		public override string GetBindingName()
		{
			return "Texture3D<" + UbershaderGenerator.GetStructFieldHLSLType(type, false) + ">";
		}
	}
}
