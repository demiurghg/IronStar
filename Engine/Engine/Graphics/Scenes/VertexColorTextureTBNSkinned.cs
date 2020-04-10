﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics.Scenes 
{
	public struct VertexColorTextureTBNSkinned 
	{

		[Vertex("POSITION")]		public Vector3	Position;
		[Vertex("TANGENT")]			public Half4	Tangent	;
		[Vertex("BINORMAL")]		public Half4	Binormal;
		[Vertex("NORMAL")]			public Half4	Normal	;
		[Vertex("COLOR")]			public Color	Color	;
		[Vertex("TEXCOORD")]		public Vector2	TexCoord;
		[Vertex("BLENDINDICES")]	public Int4		SkinIndices;
		[Vertex("BLENDWEIGHTS")]	public Vector4	SkinWeights;


		static public VertexInputElement[] Elements {
			get {
				return VertexInputElement.FromStructure( typeof(VertexColorTextureTBNSkinned) );
			}
		}


		public static VertexColorTextureTBNSkinned Convert ( MeshVertex meshVertex )
		{
			VertexColorTextureTBNSkinned v;
			v.Position		=	meshVertex.Position;
			v.Tangent		=	MathUtil.ToHalf4( meshVertex.Tangent,	0 );
			v.Binormal		=	MathUtil.ToHalf4( meshVertex.Binormal,	0 );	
			v.Normal		=	MathUtil.ToHalf4( meshVertex.Normal,		0 );	
			v.Color			=	meshVertex.Color0;
			v.TexCoord		=	meshVertex.TexCoord0;
			v.SkinIndices	=	meshVertex.SkinIndices;
			v.SkinWeights	=	meshVertex.SkinWeights;
			return v;
		}
	}
}
