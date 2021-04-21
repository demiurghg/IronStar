using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion;
using IronStar.Mapping;
using IronStar.Editor.Manipulators;
using IronStar.Editor;

namespace IronStar.Editor.Manipulators 
{
	public class MoveHandle : Handle 
	{
		readonly Vector3 axis;
		readonly Color color;
		readonly Action<Vector3> move;
		readonly bool local;

		Matrix  initialTransform;
		float	snapping;
		Vector3	initialPoint;
		Vector3 currentPoint;


		public MoveHandle ( MapEditor editor, Vector3 axis, bool local, Color color, Action<Vector3> move ) : base(editor)
		{
			this.axis	=	axis.Normalized();
			this.color	=	color;
			this.move	=	move;
			this.local	=	local;
		}


		public override void Draw( Matrix transform, State state )
		{
			Color drawColor = color;
			if (state==State.Active) drawColor = Utils.ActiveHandleColor;
			if (state==State.Inactive) drawColor = Utils.InactiveHandleColor;
			if (state==State.Highlighted) drawColor = Utils.HighlightedHandleColor;

			var currentAxis = local ? Vector3.TransformNormal( axis, transform ) : axis;
			
			DrawArrow( transform.TranslationVector, currentAxis, drawColor );
		}


		public override HandleIntersection Intersect( Matrix transform, Point pickPoint )
		{
			var currentAxis = local ? Vector3.TransformNormal( axis, transform ) : axis;

			return IntersectArrow( transform.TranslationVector, currentAxis, pickPoint );
		}


		public override void Start( Matrix transform, Point pickPoint, float snapping )
		{
			var currentAxis		=	local ? Vector3.TransformNormal( axis, transform ) : axis;

			this.snapping		=	snapping;
			initialTransform	=	transform;
			var result		= IntersectArrow( initialTransform.TranslationVector, currentAxis, pickPoint ); 

			initialPoint	=	result.HitPoint;
			currentPoint	=	result.HitPoint;
		}


		void MoveInternal( Vector3 moveVector, Vector3 currentAxis )
		{
			var initialPosistion	=	initialTransform.TranslationVector;
			var currentPosition		=	Manipulator.Snap( initialPosistion + (moveVector), snapping ); 
			var translationDelta	=	currentPosition - initialPosistion;

			moveVector = currentAxis * Vector3.Dot( translationDelta, currentAxis );

			move( moveVector );
		}


		public override void Update( Point pickPoint )
		{
			var currentAxis	=	local ? Vector3.TransformNormal( axis, initialTransform ) : axis;
			var result		=	IntersectArrow( initialTransform.TranslationVector, currentAxis, pickPoint ); 
			var delta		=	result.HitPoint - initialPoint;
			MoveInternal( delta, currentAxis );
		}


		public override void Stop( Point pickPoint )
		{
		}
	}
}
