using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BEPUrender
{
	public class Model
	{
		public readonly List<Mesh> Meshes = new List<Mesh>();
	}

	public class Mesh
	{
		public readonly List<Effect> Effects = new List<Effect>();
	}

	public class Effect : IDisposable
	{
		public void Dispose(){}
	}

	public class BasicEffect : Effect 
	{
		public BasicEffect(object dummy) {}
        public bool LightingEnabled;
        public bool VertexColorEnabled;
        public bool TextureEnabled;
        public void EnableDefaultLighting() {}
        public Fusion.Core.Mathematics.Matrix World;
		public Fusion.Core.Mathematics.Matrix View;
		public Fusion.Core.Mathematics.Matrix Projection;
	}

	public class BlendState : IDisposable
	{
		public void Dispose(){}
	}

	public enum PrimitiveType
	{
		LineList, TriangleList
	}

	public enum Blend
	{
		SourceAlpha, InverseSourceAlpha,
	}

	public class Texture2D{	}
}
