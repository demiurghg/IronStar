using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics.Ubershaders
{
	/*------------------------------------------------------------------------------------------------
	 *	Shader Handle
	------------------------------------------------------------------------------------------------*/

	abstract internal class FXHandle 
	{
		protected readonly int  register;
		protected readonly Type type;
		protected readonly string name;

		public FXHandle(int register, string name, Type type)
		{
			this.register	= register;
			this.type		= type;
			this.name		= name;
		}

		public abstract string GetDeclaration();

		public static implicit operator int (FXHandle handle) { return handle.register; }

		public static Type[] GatherStructureTypes (	IEnumerable<FXHandle> handles )
		{
			return handles
				.Select( h0 => h0.type )
				.Where( h1 => h1!=null)
				.Where( h2 => h2.IsStruct() )
				.Where( h3 => !UbershaderGenerator.IsHLSLSupportedType(h3) )
				.Distinct()
				.ToArray();
		}
	}


	/*------------------------------------------------------------------------------------------------
	 *	Register Types
	------------------------------------------------------------------------------------------------*/

	struct CRegister
	{
		public readonly int Index;
		public readonly int Array;
		public readonly string Name;
		public CRegister(int reg, int array, string name ) { Index = reg; Name = name; Array = array; }
		public CRegister(int reg, string name) { Index = reg; Name = name; Array = 0; }
	}

	struct SRegister
	{
		public readonly int Index;
		public readonly string Name;
		public SRegister(int reg, string name) { Index = reg; Name = name; }
	}

	struct TRegister
	{
		public readonly int Index;
		public readonly string Name;
		public TRegister(int reg, string name) { Index = reg; Name = name; }
	}

	class URegister
	{
		public readonly int Index;
		public readonly string Name;
		public URegister(int reg, string name) { Index = reg; Name = name; }
	}

	/*------------------------------------------------------------------------------------------------
	 *	
	------------------------------------------------------------------------------------------------*/

	internal sealed class FXStructure<T> : FXHandle
	{
		public FXStructure() : base( 0, "", typeof(T) )
		{
		}

		public override string GetDeclaration()
		{
			return "";
		}

		public static implicit operator FXStructure<T>(int zero) 
		{
			return new FXStructure<T>();
		}
	}

	/*------------------------------------------------------------------------------------------------
	 *	Constant Buffer Handle
	------------------------------------------------------------------------------------------------*/

	internal sealed class FXConstantBuffer<T> : FXHandle
	{
		readonly int array;

		public FXConstantBuffer(int register, string name, int array=0) : base(register, name, typeof(T))
		{
			this.array	=	array;
		}

		public override string GetDeclaration()
		{
			var arrayDecl = array > 0 ? "[" + array.ToString() + "]" : "";
			return string.Format("cbuffer __buffer{0} : register(b{0}) {{\r\n\t{1} {2}{3} : packoffset(c0);\r\n}};", 
				register, UbershaderGenerator.GetStructFieldHLSLType(type, true), name, arrayDecl );
		}

		public static implicit operator FXConstantBuffer<T>(CRegister reg) 
		{
			return new FXConstantBuffer<T>(reg.Index, reg.Name, reg.Array);
		}
	}


	/*------------------------------------------------------------------------------------------------
	 *	Sampler State Handle
	------------------------------------------------------------------------------------------------*/

	internal sealed class FXSamplerState : FXHandle
	{
		public FXSamplerState(int register, string name) : base(register, name, null)
		{
		}

		public override string GetDeclaration()
		{
			return string.Format("{0,-30} {1,-30} : register(s{2});", "SamplerState", name, register);
		}

		public static implicit operator FXSamplerState(SRegister reg) 
		{
			return new FXSamplerState(reg.Index, reg.Name);
		}
	}

	internal sealed class FXSamplerComparisonState : FXHandle
	{
		public FXSamplerComparisonState(int register, string name) : base(register, name, null)
		{
		}

		public override string GetDeclaration()
		{
			return string.Format("{0,-30} {1,-30} : register(s{2});", "SamplerComparisonState", name, register);
		}

		public static implicit operator FXSamplerComparisonState(SRegister reg) 
		{
			return new FXSamplerComparisonState(reg.Index, reg.Name);
		}
	}

	/*------------------------------------------------------------------------------------------------
	 *	Texture Handles
	------------------------------------------------------------------------------------------------*/

	internal abstract class FXShaderResource : FXHandle
	{
		public FXShaderResource(int register, string name, Type type) : base(register, name, type ?? typeof(Vector4))
		{
		}

		protected abstract string GetResourceTypeName();

		public override string GetDeclaration()
		{
			var resourceType	=	GetResourceTypeName();
			var isReadWrite		=	resourceType.StartsWith("RW") || resourceType.StartsWith("Append") || resourceType.StartsWith("Consume");
			var isStructured	=	resourceType.Contains("Structured");
			var byteAddress		=	resourceType.Contains("ByteAddress");

			var dataTypeName	=	byteAddress ? "" : "<" + UbershaderGenerator.GetStructFieldHLSLType(type, isStructured) + ">";

			var registerName	=	(isReadWrite ? "u" : "t") + register.ToString();

			var declaringType	=	resourceType + dataTypeName;

			return string.Format("{0,-30} {1,-30} : register({2});", declaringType, name, registerName );
		}
	}


	internal sealed class FXTexture2D<T> : FXShaderResource
	{
		public FXTexture2D (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXTexture2D<T>(TRegister reg) { return new FXTexture2D<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "Texture2D"; }
	}

	internal sealed class FXTexture2DArray<T> : FXShaderResource
	{
		public FXTexture2DArray (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXTexture2DArray<T>(TRegister reg) { return new FXTexture2DArray<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "Texture2DArray"; }
	}

	internal sealed class FXTextureCube<T> : FXShaderResource
	{
		public FXTextureCube (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXTextureCube<T>(TRegister reg) { return new FXTextureCube<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "TextureCube"; }
	}

	internal sealed class FXTextureCubeArray<T> : FXShaderResource
	{
		public FXTextureCubeArray (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXTextureCubeArray<T>(TRegister reg) { return new FXTextureCubeArray<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "TextureCubeArray"; }
	}

	internal sealed class FXTexture3D<T> : FXShaderResource
	{
		public FXTexture3D (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXTexture3D<T>(TRegister reg) { return new FXTexture3D<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "Texture3D"; }
	}

	internal sealed class FXByteAddressBuffer : FXShaderResource
	{
		public FXByteAddressBuffer (int register, string name) : base(register, name, typeof(byte)) {}
		public static implicit operator FXByteAddressBuffer(TRegister reg) { return new FXByteAddressBuffer(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "ByteAddressBuffer"; }
	}

	internal sealed class FXBuffer<T> : FXShaderResource
	{
		public FXBuffer (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXBuffer<T>(TRegister reg) { return new FXBuffer<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "Buffer"; }
	}

	internal sealed class FXStructuredBuffer<T> : FXShaderResource
	{
		public FXStructuredBuffer (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXStructuredBuffer<T>(TRegister reg) { return new FXStructuredBuffer<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "StructuredBuffer"; }
	}



	internal sealed class FXRWTexture2D<T> : FXShaderResource
	{
		public FXRWTexture2D (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXRWTexture2D<T>(URegister reg) { return new FXRWTexture2D<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "RWTexture2D"; }
	}

	internal sealed class FXRWTexture2DArray<T> : FXShaderResource
	{
		public FXRWTexture2DArray (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXRWTexture2DArray<T>(URegister reg) { return new FXRWTexture2DArray<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "RWTexture2DArray"; }
	}

	internal sealed class FXRWTexture3D<T> : FXShaderResource
	{
		public FXRWTexture3D (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXRWTexture3D<T>(URegister reg) { return new FXRWTexture3D<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "RWTexture3D"; }
	}

	internal sealed class FXRWByteAddressBuffer : FXShaderResource
	{
		public FXRWByteAddressBuffer (int register, string name) : base(register, name, typeof(byte)) {}
		public static implicit operator FXRWByteAddressBuffer(URegister reg) { return new FXRWByteAddressBuffer(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "RWByteAddressBuffer"; }
	}

	internal sealed class FXRWBuffer<T> : FXShaderResource
	{
		public FXRWBuffer (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXRWBuffer<T>(URegister reg) { return new FXRWBuffer<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "RWBuffer"; }
	}

	internal sealed class FXRWStructuredBuffer<T> : FXShaderResource
	{
		public FXRWStructuredBuffer (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXRWStructuredBuffer<T>(URegister reg) { return new FXRWStructuredBuffer<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "RWStructuredBuffer"; }
	}

	internal sealed class FXAppendStructuredBuffer<T> : FXShaderResource
	{
		public FXAppendStructuredBuffer (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXAppendStructuredBuffer<T>(URegister reg) { return new FXAppendStructuredBuffer<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "AppendStructuredBuffer"; }
	}

	internal sealed class FXConsumeStructuredBuffer<T> : FXShaderResource
	{
		public FXConsumeStructuredBuffer (int register, string name) : base(register, name, typeof(T)) {}
		public static implicit operator FXConsumeStructuredBuffer<T>(URegister reg) { return new FXConsumeStructuredBuffer<T>(reg.Index, reg.Name); }
		protected override string GetResourceTypeName() { return "ConsumeStructuredBuffer"; }
	}
}
