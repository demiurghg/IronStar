using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Fusion.Core.Mathematics;
using System.Runtime.InteropServices;
using Fusion.Drivers.Graphics;
using System.Globalization;
using Fusion.Build.Processors;
using Fusion.Build;
using Fusion.Core.Extensions;
using System.IO;

namespace Fusion.Engine.Graphics.Ubershaders {
	public class UbershaderGenerator : AssetGenerator 
	{

		public override IEnumerable<AssetSource> Generate( IBuildContext context, BuildResult result )
		{
			var srcList = new List<AssetSource>();

			var shaderList = Misc.GetAllClassesWithAttribute<RequireShaderAttribute>()
					.Select( type1 => new { Type=type1, ShaderAttr=type1.GetCustomAttribute<RequireShaderAttribute>() } )
					.GroupBy( shader => shader.ShaderAttr.RequiredShader )
					.ToArray();

			foreach ( var shaderGroup in shaderList )
			{
				var name = shaderGroup.First().ShaderAttr.RequiredShader;

				var gen  = shaderGroup.Any( sg => sg.ShaderAttr.AutoGenerateHeader );

				if (!gen) continue;

				var nameExt = Path.ChangeExtension( name, ".hlsl" );

				try
				{
					var headerTextBuilder	=	new StringBuilder();
					var baseDir				=   "";
					var fullPath			=   context.ResolveContentPath( nameExt, out baseDir );
					var assetSrc			=   new AssetSource( nameExt, baseDir, context.FullOutputDirectory, new UbershaderProcessor(), context );

					headerTextBuilder.Append( GenerateVirtualHeader( typeof(RenderSystem), null ) );

					foreach ( var shader in shaderGroup )
					{
						if ( shader.ShaderAttr.AutoGenerateHeader )
						{
							headerTextBuilder.Append( GenerateVirtualHeader( shader.Type, shader.ShaderAttr.IfDefined ) );
						}
					}

					var headerName = assetSrc.FullSourcePath.Replace(nameExt, "\\auto\\" + name + ".fxi");
					var headerText = headerTextBuilder.ToString();

					if ( !File.Exists( headerName ) )
					{
						Directory.CreateDirectory( Path.GetDirectoryName( headerName ) );
						File.WriteAllText( headerName, headerText );
					}
					else
					{
						var oldText = File.ReadAllText( headerName );
						if ( oldText!=headerText )
						{
							File.WriteAllText( headerName, headerText );
						}
					}

					srcList.Add( assetSrc );
					result.Total++;
				}
				catch ( BuildException bex )
				{
					Log.Error( bex.Message );
				}

			}

			return srcList;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		static public string GenerateVirtualHeader ( Type type, string ifdef )
		{
			var sb = new StringBuilder();

			ReflectDefinitions( sb, type );
			ReflectFXHandles( sb, type );

			return sb.ToString();
		}


		static void ReflectFXHandles( StringBuilder sb, Type type )
		{
			var handles = type
				.GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)
				.Where( fi0 => fi0.FieldType.IsSubclassOf( typeof(FXHandle) ) )
				.Select( fi1 => new { Handle = (FXHandle)fi1.GetValue(null), Define = fi1.GetCustomAttribute<ShaderIfDefAttribute>()?.Define } )
				.ToArray();
		
			var structs		=	FXHandle.GatherStructureTypes( handles.Select( h => h.Handle ) );
			
			AppendSplitter(sb, "Data Structures");

			foreach ( var structure in structs ) 
			{
				ReflectStructure( sb, structure );
			}
			
			AppendSplitter(sb, "Shader Resources");

			var groupedHandles = handles
				.GroupBy( h0 => h0.Define )
				.Select( g0 => new { Defines = g0.First().Define, Handles = g0.Select(h1 => h1.Handle).ToArray() } )
				.ToArray();

			foreach ( var group in groupedHandles )
			{
				if (group.Defines!=null) 
				{
					var ifdef = string.Join( " || ", group.Defines.Split(' ',',','|').Select( def => "defined(" + def + ")" ) );
					sb.AppendLine("#if " + ifdef);
				}

				foreach ( var handle in group.Handles )
				{
					sb.AppendLine( handle.GetDeclaration() );
				}

				if (group.Defines!=null) sb.AppendLine("#endif");
			}
		}


		public static PropertyInfo[] GetSamplerProperties ( Type type )
		{
			var list = type
				.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Where( p1 => p1.GetCustomAttribute<ShaderSamplerAttribute>() != null )
				.Where( p2 => p2.PropertyType.IsSubclassOf(typeof(SamplerState)) || p2.PropertyType == typeof(SamplerState) )
				.ToArray();

			return list;
		}


		public static PropertyInfo[] GetConstantBufferProperties ( Type type )
		{
			var list = type
				.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Where( p1 => p1.GetCustomAttribute<ShaderConstantBufferAttribute>() != null )
				.Where( p2 => p2.PropertyType.IsSubclassOf(typeof(ConstantBuffer)) || p2.PropertyType == typeof(ConstantBuffer) )
				.ToArray();

			return list;
		}


		public static PropertyInfo[] GetShaderResourceProperties ( Type type )
		{
			var list = type
				.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Where( p1 => p1.GetCustomAttribute<ShaderResourceAttribute>() != null )
				.Where( p2 => p2.PropertyType.IsSubclassOf(typeof(ShaderResource)) || p2.PropertyType == typeof(ShaderResource) )
				.ToArray();

			return list;
		}


		public static PropertyInfo[] GetUnorderedAccessProperties ( Type type )
		{
			var list = type
				.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Where( p1 => p1.GetCustomAttribute<UnorderedAccessAttribute>() != null )
				.Where( p2 => p2.PropertyType.IsSubclassOf(typeof(UnorderedAccess)) || p2.PropertyType == typeof(UnorderedAccess) )
				.ToArray();

			return list;
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Text generating :
		 * 
		-----------------------------------------------------------------------------------------*/

		static void AppendSplitter( StringBuilder sb, string text )
		{
			sb.AppendFormat("// ---------------- {0} ---------------- //\r\n\r\n", text.ToUpper());
		}


		static void ReflectResources ( StringBuilder sb, Type type )
		{
			AppendSplitter(sb, "Shader Resources");

			int register = 0;

			foreach ( var prop in GetShaderResourceProperties(type) ) {

				var srvAttr = prop.GetCustomAttributeInheritedFrom<ShaderResourceAttribute>();

				sb.AppendFormat("{0,-30} {1,-30} : register(t{2});\r\n", srvAttr.GetBindingName(), prop.Name, register++);
			}

			sb.AppendFormat("\r\n");
		}



		static void ReflectConstantBuffers ( StringBuilder sb, Type type )
		{
			AppendSplitter(sb, "Constant Buffers");

			int register = 0;

			foreach ( var prop in GetConstantBufferProperties(type) ) {

				var cbAttr = prop.GetCustomAttribute<ShaderConstantBufferAttribute>();

				var array = (cbAttr.ArraySize==0) ? "" : "[" + cbAttr.ArraySize.ToString() + "]";

				sb.AppendFormat("cbuffer __buffer{0} : register(b{0}) {{\r\n", register );
				sb.AppendFormat("\t{0} {1}{2} : packoffset(c0);\r\n", cbAttr.ConstantType.Name, prop.Name, array);
				sb.AppendFormat("}};\r\n");

				register++;
			}

			sb.AppendFormat("\r\n");
		}



		static void ReflectSamplers ( StringBuilder sb, Type type )
		{
			AppendSplitter(sb, "Samplers");

			int register = 0;

			foreach ( var prop in GetSamplerProperties(type) ) {

				var smAttr = prop.GetCustomAttribute<ShaderSamplerAttribute>();

				if (prop.PropertyType!=typeof(SamplerState)) {
					throw new ArgumentException(string.Format("Property {0} must be SamplerState", prop.Name));
				}

				var sampler = smAttr.IsComparison ? "SamplerComparisonState" : "SamplerState";

				sb.AppendFormat("{0,-30} {1,-30} : register(s{2});\r\n", sampler, prop.Name, register++);
			}

			sb.AppendFormat("\r\n");
		}



		[Obsolete]
		static IEnumerable<Type> ReflectStructuresForExport ( Type type )
		{
			var structs = new List<Type>();

			var sharedStructsAttrs = type.GetCustomAttributes<ShaderSharedStructureAttribute>().ToArray();

			var structTypes = sharedStructsAttrs.SelectMany( attr => attr.StructTypes ).Distinct().ToArray();

			foreach ( var structType in structTypes ) 
			{
				structs.Add( structType );
			}

			foreach ( var member in type.GetMembers(BindingFlags.NonPublic|BindingFlags.Public) ) 
			{
				if (member.GetCustomAttributes().Any( attr => attr is ShaderStructureAttribute )) 
				{
					if (member.MemberType==MemberTypes.NestedType) 
					{
						var nestedType	=  (Type)member;
						structs.Add( nestedType );
					}
				}
			}

			return structs;
		}



		static int SizeOf( Type type )
		{
			if (type.IsEnum) {
				return Marshal.SizeOf(Enum.GetUnderlyingType(type));
			} else {
				return Marshal.SizeOf(type);
			}
		}



		static void ReflectStructure ( StringBuilder sb, Type nestedType )
		{			
            //	https://msdn.microsoft.com/en-us/library/windows/desktop/bb509632(v=vs.85).aspx
			//CheckAlligmentRules(nestedType);

			sb.AppendFormat("// {0}\r\n", nestedType);
			sb.AppendFormat("// Marshal.SizeOf = {0}\r\n", Marshal.SizeOf(nestedType));
			sb.AppendFormat("#define {0}_DEFINED 1\r\n", nestedType.Name);
			sb.AppendFormat("struct {0} {{\r\n", nestedType.Name);

			foreach ( var field in nestedType.GetFields() ) {

				var offset	=	Marshal.OffsetOf( nestedType, field.Name );
				var size	=	SizeOf( field.FieldType );

                sb.AppendFormat("\t{0,-10} {1,-30} // offset: {2,4}\r\n", GetStructFieldHLSLType(field.FieldType), field.Name + ";", offset );
			}

			sb.AppendFormat("}};\r\n");
			sb.AppendFormat("\r\n");
		}



		static void ReflectDefinitions ( StringBuilder sb, Type type )
		{
			AppendSplitter(sb, "Constant Values");

			foreach ( var field in type.GetFields(BindingFlags.Instance | 
                       BindingFlags.Static |
                       BindingFlags.NonPublic |
                       BindingFlags.Public) ) {

				if (field.GetCustomAttribute<ShaderDefineAttribute>()==null) {
					continue;
				}

				if (field.IsLiteral) {

					string value;
					var culture	= CultureInfo.InvariantCulture;

					if (field.FieldType==typeof(int))			value = ((int)  field.GetValue(null)).ToString(culture);
					else if (field.FieldType==typeof(float))	value = ((float)field.GetValue(null)).ToString(culture);
					else if (field.FieldType==typeof(uint))		value = ((uint) field.GetValue(null)).ToString(culture);
					else throw new Exception(string.Format("Bad type for HLSL definition : {0}", field.FieldType));

					var typeName = GetStructFieldHLSLType( field.FieldType );

					sb.AppendFormat("static const {0} {1} = {2};\r\n", typeName, field.Name, value);
				}
			}

			sb.AppendFormat("\r\n");
		}


		internal static bool IsHLSLSupportedType( Type type )
		{
			if (type==typeof( int ))		return true;
			if (type==typeof( uint ))		return true;
			if (type==typeof( float ))		return true;
			if (type==typeof( Vector2 ))	return true;
			if (type==typeof( Vector3 ))	return true;
			if (type==typeof( Vector4 ))	return true;
			if (type==typeof( Plane ))		return true;
			//if (type==typeof( Half ))		return true;
			if (type==typeof( Half2 ))		return true;
			//if (type==typeof( Half3 ))		return true;
			if (type==typeof( Half4 ))		return true;
			if (type==typeof( Int2 ))		return true;
			if (type==typeof( Int3 ))		return true;
			if (type==typeof( Int4 ))		return true;
			if (type==typeof( UInt2 ))		return true;
			if (type==typeof( UInt3 ))		return true;
			if (type==typeof( UInt4 ))		return true;
			if (type==typeof( Color3 ))		return true;
			if (type==typeof( Color4 ))		return true;
			if (type==typeof( Matrix ))		return true;
			return false;
		}


		internal static string GetStructFieldHLSLType ( Type type, bool allowStructs=false )
		{
			if (type==typeof( int )) return "int";
			if (type==typeof( uint )) return "uint";
			if (type==typeof( float )) return "float";
			if (type==typeof( Vector2 )) return "float2";
			if (type==typeof( Vector3 )) return "float3";
			if (type==typeof( Vector4 )) return "float4";
			if (type==typeof( Plane )) return "float4";
			//if (type==typeof( Half )) return  "half";
			//if (type==typeof( Half2 )) return "half2";
			//if (type==typeof( Half3 )) return "half3";
			if (type==typeof( Half2 )) return "uint";
			if (type==typeof( Half4 )) return "uint2";
			if (type==typeof( Int2 )) return "int2";
			if (type==typeof( Int3 )) return "int3";
			if (type==typeof( Int4 )) return "int4";
			if (type==typeof( UInt2 )) return "uint2";
			if (type==typeof( UInt3 )) return "uint3";
			if (type==typeof( UInt4 )) return "uint4";
			if (type==typeof( Color3 )) return "float3";
			if (type==typeof( Color4 )) return "float4";
			if (type==typeof( Matrix )) return "float4x4";
			if (type.IsEnum) return GetStructFieldHLSLType(Enum.GetUnderlyingType(type));

			if (allowStructs) 
			{
				if (type.IsStruct())
				{
					return type.Name;	
				}
			}

			throw new Exception(string.Format("Bad HLSL type {0}", type));
		}



        private static void CheckAlligmentRules(Type type)
        {
            var fields = type.GetFields().ToList();
            fields.Sort((a, b) => Marshal.OffsetOf(type, a.Name).ToInt32().CompareTo(Marshal.OffsetOf(type, b.Name).ToInt32()));
            int accOffset = 0;

            foreach (var fi in fields) {
                int size = Marshal.SizeOf(fi.FieldType);
                int curOffset = Marshal.OffsetOf(type, fi.Name).ToInt32();

                if (accOffset / 16 == curOffset / 16 && accOffset % 16 != 0 && accOffset % 16 + size > 16)
                {
                    throw new ArgumentException($"Field {fi.Name} in struct {type.Name} has wrong offset {curOffset}. Offset must be {accOffset / 16 * 17}");
                }
                accOffset = curOffset + size;
            }
        }
	}
}
