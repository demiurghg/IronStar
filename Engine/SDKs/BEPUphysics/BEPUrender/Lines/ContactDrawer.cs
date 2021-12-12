using BEPUutilities.DataStructures;
using Fusion.Engine.Graphics.Scenes;
using BEPUphysics;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Engine.Graphics;

namespace BEPUrender.Lines
{
    /// <summary>
    /// Renders contact points.
    /// </summary>
    public class ContactDrawer
    {
        public void Draw(DebugRender debugRender, Space space)
        {
            int contactCount = 0;

            foreach (var pair in space.NarrowPhase.Pairs)
            {
                var pairHandler = pair as CollidablePairHandler;
                if (pairHandler != null)
                {
                    foreach (ContactInformation information in pairHandler.Contacts)
                    {
                        contactCount++;
                        if (information.Contact.PenetrationDepth < 0)
                        {
                            debugRender.PushVertex(new DebugVertex(MathConverter.Convert(information.Contact.Position), Color.Blue));
                            debugRender.PushVertex(new DebugVertex(MathConverter.Convert(information.Contact.Position + information.Contact.Normal * information.Contact.PenetrationDepth), Color.White));
                            debugRender.PushVertex(new DebugVertex(MathConverter.Convert(information.Contact.Position), Color.White));
                            debugRender.PushVertex(new DebugVertex(MathConverter.Convert(information.Contact.Position + information.Contact.Normal * .3f), Color.White));
                        }
                        else
                        {
                            debugRender.PushVertex(new DebugVertex(MathConverter.Convert(information.Contact.Position), Color.White));
                            debugRender.PushVertex(new DebugVertex(MathConverter.Convert(information.Contact.Position + information.Contact.Normal * information.Contact.PenetrationDepth), Color.Red));
                            debugRender.PushVertex(new DebugVertex(MathConverter.Convert(information.Contact.Position + information.Contact.Normal * information.Contact.PenetrationDepth), Color.White));
                            debugRender.PushVertex(new DebugVertex(MathConverter.Convert(information.Contact.Position + information.Contact.Normal * (information.Contact.PenetrationDepth + .3f)), Color.White));
                        }

                    }
                }
            }
        }
    }
}
