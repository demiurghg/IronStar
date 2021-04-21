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
	public class RotateHandle : Handle 
	{
		readonly Vector3 axis;
		readonly Color color;
		readonly Action<Quaternion> rotate;
		readonly bool local;

		Matrix  initialTransform;
		float	snapping;
		Vector3	initialPoint;
		Vector3 currentPoint;


		public RotateHandle ( MapEditor editor, Vector3 axis, bool local, Color color, Action<Quaternion> rotate ) : base(editor)
		{
			this.axis	=	axis.Normalized();
			this.color	=	color;
			this.rotate	=	rotate;
			this.local	=	local;
		}


		public override void Draw( Matrix transform, State state )
		{
			Color drawColor = color;
			if (state==State.Active) drawColor = Utils.ActiveHandleColor;
			if (state==State.Inactive) drawColor = Utils.InactiveHandleColor;
			if (state==State.Highlighted) drawColor = Utils.HighlightedHandleColor;

			var currentAxis = local ? Vector3.TransformNormal( axis, transform ) : axis;
			
			DrawRing( transform.TranslationVector, currentAxis, drawColor );
		}


		public override HandleIntersection Intersect( Matrix transform, Point pickPoint )
		{
			var currentAxis = local ? Vector3.TransformNormal( axis, transform ) : axis;

			return IntersectRing( transform.TranslationVector, currentAxis, pickPoint );
		}


		public override void Start( Matrix transform, Point pickPoint, float snapping )
		{
			var currentAxis		=	local ? Vector3.TransformNormal( axis, transform ) : axis;

			this.snapping		=	snapping;
			initialTransform	=	transform;
			var result			=	IntersectRing( initialTransform.TranslationVector, currentAxis, pickPoint ); 

			initialPoint	=	result.HitPoint;
			currentPoint	=	result.HitPoint;
		}


		void RotateInternal( float angle, Vector3 currentAxis )
		{
			//var initialPosistion	=	initialTransform.TranslationVector;
			//var currentPosition		=	Manipulator.Snap( initialPosistion + (moveVector), snapping ); 
			//var translationDelta	=	currentPosition - initialPosistion;
			angle	=	MathUtil.DegreesToRadians( angle );

			rotate( Quaternion.RotationAxis( currentAxis, angle ) );
		}


		public override void Update( Point pickPoint )
		{
			var currentAxis	=	local ? Vector3.TransformNormal( axis, initialTransform ) : axis;
			var result		=	IntersectRing( initialTransform.TranslationVector, currentAxis, pickPoint ); 
			var origin		=	initialTransform.TranslationVector;
			currentPoint	=	result.HitPoint;

			var	vector0		=	(initialPoint - origin).Normalized();
			var	vector1		=	(currentPoint - origin).Normalized();

			var sine		=	Vector3.Dot( currentAxis, Vector3.Cross( vector0, vector1 ) );
			var cosine		=	Vector3.Dot( vector0, vector1 );

			var angle		=	MathUtil.RadiansToDegrees ( (float)Math.Atan2( sine, cosine ) );
				angle		=	Manipulator.Snap( angle, snapping );

			RotateInternal( angle, currentAxis );
		}


		public override void Stop( Point pickPoint )
		{
		}
	}
}
