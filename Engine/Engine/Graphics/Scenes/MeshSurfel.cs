using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SharpDX;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;


namespace Fusion.Engine.Graphics.Scenes {

	public struct MeshSurfel : IEquatable<MeshSurfel> {

		/// <summary>
		/// XYZ postiion
		/// </summary>
		public	Vector3		Position	;

		/// <summary>
		/// Surfel normal
		/// </summary>
		public	Vector3		Normal		;

		/// <summary>
		/// Approximate surfel area
		/// </summary>
		public	float		Area	;

		/// <summary>
		/// Surfel albedo.
		/// </summary>
		public	Color		Albedo	;



		public bool Equals(MeshSurfel other) 
		{
			return (this.Position	==	other.Position	 
				 && this.Normal		==	other.Normal		 
				 && this.Area		==	other.Area	 
				 && this.Albedo		==	other.Albedo
				 );
		}


		public override bool Equals(Object obj)
		{
            if (ReferenceEquals(null, obj)) return false;
            return obj is MeshSurfel && Equals((MeshSurfel) obj);
		}   


		public override int GetHashCode()
		{
			return Misc.FastHash( Position, Normal, Area, Albedo );
		}


		public static bool operator == (MeshSurfel vertex1, MeshSurfel vertex2)
		{
			return vertex1.Equals(vertex2);
		}


		public static bool operator != (MeshSurfel vertex1, MeshSurfel vertex2)
		{
			return ! (vertex1.Equals(vertex2));
		}
	}
}
