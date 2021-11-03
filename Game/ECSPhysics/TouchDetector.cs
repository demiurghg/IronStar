using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.ECSPhysics
{
	public class TouchDetector : IComponent, IEnumerable<Entity>
	{
		public readonly List<Entity> touches;

		public void AddTouch( Entity touch )
		{
			if (touch!=null)
			{
				touches.Add( touch );
			}
		}

		public void ClearTouches()
		{
			touches.Clear();
		}

		private TouchDetector( IEnumerable<Entity> touches )
		{
			this.touches = new List<Entity>( touches );
		}

		public TouchDetector()
		{
			touches = new List<Entity>();
		}

		public IEnumerator<Entity> GetEnumerator()
		{
			return ( (IEnumerable<Entity>)touches ).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<Entity>)touches ).GetEnumerator();
		}
	
		

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			#warning TODO : SAVE AttachmentComponent
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			#warning TODO : LOAD AttachmentComponent
		}

		public IComponent Clone()
		{
			return new TouchDetector( touches );
		}

		public IComponent Interpolate( IComponent previous, float factor )
		{
			return Clone();
		}
	}
}
