﻿using BEPUphysics.Constraints.TwoEntity.JointLimits;
using Fusion.Core.Mathematics;
using Fusion.Core;
using MathConverter = Fusion.Core.Mathematics.MathConverter;

namespace BEPUrender.Lines
{
    /// <summary>
    /// Graphical representation of a PointOnPlaneConstraint
    /// </summary>
    public class DisplayDistanceLimit : SolverDisplayObject<DistanceLimit>
    {
        private readonly Line aToConnection;
        private readonly Line bToConnection;
        private readonly Line error;

        public DisplayDistanceLimit(DistanceLimit constraint, LineDrawer drawer)
            : base(drawer, constraint)
        {
            aToConnection = new Line(Color.DarkBlue, Color.DarkBlue, drawer);
            bToConnection = new Line(Color.DarkBlue, Color.DarkBlue, drawer);
            error = new Line(Color.Red, Color.Red, drawer);
            myLines.Add(aToConnection);
            myLines.Add(bToConnection);
            myLines.Add(error);
        }


        /// <summary>
        /// Moves the constraint lines to the proper location relative to the entities involved.
        /// </summary>
        public override void Update()
        {
            //Move lines around
            aToConnection.PositionA = MathConverter.Convert(LineObject.ConnectionA.Position);
            aToConnection.PositionB = MathConverter.Convert(LineObject.WorldAnchorA);

            bToConnection.PositionA = MathConverter.Convert(LineObject.ConnectionB.Position);
            bToConnection.PositionB = MathConverter.Convert(LineObject.WorldAnchorB);

            error.PositionA = aToConnection.PositionB;
            error.PositionB = bToConnection.PositionB;
        }
    }
}