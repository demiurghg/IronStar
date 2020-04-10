using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using System.Reflection;


namespace Fusion.Engine.Graphics {

	public class RenderComponent : DisposableBase, IGameComponent, IPipelineStateProvider
	{
		protected readonly Game Game;
		protected readonly RenderSystem rs;
		protected readonly GraphicsDevice device;

		/// <summary>
		/// Creates instance of render pass 
		/// </summary>
		public RenderComponent ( RenderSystem rs )
		{
			this.rs		=	rs;
			this.Game	=	rs.Game;
			this.device	=	rs.Game.GraphicsDevice;

			InitializeReflectedShaderData();
		}


		/// <summary>
		/// Initializes render pass
		/// </summary>
		public virtual void Initialize ()
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public virtual bool ProvideState( PipelineState ps, int flags )
		{
			return false;
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 * 
		 * 
		-----------------------------------------------------------------------------------------*/

		class SrvBinding 
		{
			public SrvBinding( PropertyInfo pi )
			{
				Property	=	pi;
				Group		=	pi.GetCustomAttributeInheritedFrom<ShaderResourceAttribute>().Group;
			}

			public readonly PropertyInfo Property;
			public readonly uint Group;
		}

		class UavBinding 
		{
			public UavBinding( PropertyInfo pi )
			{
				Property	=	pi;
				Group		=	pi.GetCustomAttributeInheritedFrom<UnorderedAccessAttribute>().Group;
				InitCount	=	pi.GetCustomAttributeInheritedFrom<UnorderedAccessAttribute>().InitCount;
			}

			public readonly PropertyInfo Property;
			public readonly uint Group;
			public readonly int InitCount;
		}

		PropertyInfo[] samplerStateProps;
		PropertyInfo[] constantBufferProps;
		SrvBinding[] srvBindings;
		UavBinding[] uavBindings;



		void InitializeReflectedShaderData ()
		{
			samplerStateProps	= UbershaderGenerator.GetSamplerProperties( GetType() ).ToArray();
			constantBufferProps = UbershaderGenerator.GetConstantBufferProperties( GetType() ).ToArray();

			srvBindings = UbershaderGenerator.GetShaderResourceProperties( GetType() )
				.Select( pi => new SrvBinding( pi ) )
				.ToArray();

			uavBindings = UbershaderGenerator.GetUnorderedAccessProperties( GetType() )
				.Select( pi => new UavBinding( pi ) )
				.ToArray();
		}


		/// <summary>
		/// Sets reflected sampler states according to ubershader declaration
		/// </summary>
		protected void SetGfxSamplerStates (uint mask = 0xFFFFFFFF)
		{
			for ( int i=0; i<samplerStateProps.Length; i++ ) 
			{
				var samplerState = (SamplerState)samplerStateProps[i].GetValue(this);

				rs.Game.GraphicsDevice.GfxSamplers[i]		=	samplerState;
			}
		}


		/// <summary>
		/// Sets reflected constant buffers according to ubershader declaration
		/// </summary>
		protected void SetGfxConstantBuffers ()
		{
			for ( int i=0; i<constantBufferProps.Length; i++ ) 
			{
				var constBuffer = (ConstantBuffer)constantBufferProps[i].GetValue(this);

				rs.Game.GraphicsDevice.GfxConstants[i]		=	constBuffer;
			}
		}


		/// <summary>
		/// Sets reflected constant buffers according to ubershader declaration
		/// </summary>
		protected void SetGfxShaderResources ( uint groupMask )
		{
			for ( int i=0; i<srvBindings.Length; i++ ) 
			{
				if ( (srvBindings[i].Group & groupMask) != 0 ) 
				{
					var srv = (ShaderResource)srvBindings[i].Property.GetValue(this);
					rs.Game.GraphicsDevice.GfxResources[i]		=	srv;
				}
			}
		}


		/// <summary>
		/// Sets reflected constant buffers according to ubershader declaration
		/// </summary>
		protected void SetComputeShaderResources ( uint groupMask )
		{
			for ( int i=0; i<srvBindings.Length; i++ ) 
			{
				if ( (srvBindings[i].Group & groupMask) != 0 ) 
				{
					var srv = (ShaderResource)srvBindings[i].Property.GetValue(this);

					rs.Game.GraphicsDevice.ComputeResources[i]	=	srv;
				}
			}
		}


		protected void SetComputeUnorderedAccess( uint group )
		{
			for ( int i=0; i<srvBindings.Length; i++ ) 
			{
				if ( uavBindings[i].Group == group ) 
				{
					var uav		= (UnorderedAccess)uavBindings[i].Property.GetValue(this);
					var count	= uavBindings[i].InitCount;

					rs.Game.GraphicsDevice.SetComputeUnorderedAccess( i, uav, count );
				}
			}
		}
	}
}
