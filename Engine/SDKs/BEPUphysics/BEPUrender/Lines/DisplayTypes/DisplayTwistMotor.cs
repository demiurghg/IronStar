using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.TwoEntity.Motors;
using Fusion.Core.Mathematics;
using Fusion.Core;
using MathConverter = Fusion.Core.Mathematics.MathConverter;

namespace BEPUrender.Lines
{
    /// <summary>
    /// Graphical representation of a twist joint
    /// </summary>
    public class DisplayTwistMotor : SolverDisplayObject<TwistMotor>
    {
        private readonly Line axisA;
        private readonly Line axisB;


        public DisplayTwistMotor(TwistMotor constraint, LineDrawer drawer)
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
            axisA.PositionB = MathConverter.Convert(LineObject.ConnectionA.Position + LineObject.BasisA.PrimaryAxis);

            axisB.PositionA = MathConverter.Convert(LineObject.ConnectionB.Position);
            axisB.PositionB = MathConverter.Convert(LineObject.ConnectionB.Position + LineObject.BasisB.PrimaryAxis);
        }
    }
}