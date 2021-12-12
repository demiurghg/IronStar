using BEPUphysics.Constraints.TwoEntity.Joints;
using Fusion.Core.Mathematics;
using Fusion.Core;
using MathConverter = Fusion.Core.Mathematics.MathConverter;

namespace BEPUrender.Lines
{
    /// <summary>
    /// Graphical representation of a SwivelHinge.
    /// </summary>
    public class DisplaySwivelHingeAngularJoint : SolverDisplayObject<SwivelHingeAngularJoint>
    {
        private readonly Line axisA;
        private readonly Line axisB;


        public DisplaySwivelHingeAngularJoint(SwivelHingeAngularJoint constraint, LineDrawer drawer)
            : base(drawer, constraint)
        {
            axisA = new Line(Color.DarkRed, Color.DarkRed, drawer);
            axisB = new Line(Color.DarkRed, Color.DarkRed, drawer);

            myLines.Add(axisA);
            myLines.Add(axisB);
        }


        /// <summary>
        /// Moves the constraint lines to the proper location relative to the entities involved.
        /// </summary>
        public override void Update()
        {
            //Move lines around
            axisA.PositionA = MathConverter.Convert(LineObject.ConnectionA.Position);
            axisA.PositionB = MathConverter.Convert(LineObject.ConnectionA.Position + LineObject.WorldHingeAxis);

            axisB.PositionA = MathConverter.Convert(LineObject.ConnectionB.Position);
            axisB.PositionB = MathConverter.Convert(LineObject.ConnectionB.Position + LineObject.WorldTwistAxis);
        }
    }
}