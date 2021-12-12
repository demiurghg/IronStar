﻿
using BEPUphysics.Entities;
using BEPUutilities;
using MathConverter = Fusion.Core.Mathematics.MathConverter;
using Fusion.Drivers.Graphics;

namespace BEPUrender.Models
{
    /// <summary>
    /// Display object of a model that follows an entity.
    /// </summary>
    public class DisplayEntityModel : SelfDrawingModelDisplayObject
    {
        private Model myModel;

        private Texture2D myTexture;

        /// <summary>
        /// Bone transformations of meshes in the model.
        /// </summary>
        private Fusion.Core.Mathematics.Matrix[] transforms;

        /// <summary>
        /// Constructs a new display model.
        /// </summary>
        /// <param name="entity">Entity to follow.</param>
        /// <param name="model">Model to draw on the entity.</param>
        /// <param name="modelDrawer">Model drawer to use.</param>
        public DisplayEntityModel(Entity entity, Model model, ModelDrawer modelDrawer)
            : base(modelDrawer)
        {
            LocalTransform = Matrix.Identity;
            Entity = entity;
            Model = model;
        }

        /// <summary>
        /// Gets or sets the entity to base the model's world matrix on.
        /// </summary>
        public Entity Entity { get; set; }

        /// <summary>
        /// Gets or sets the model to display.
        /// </summary>
        public Model Model
        {
			get; set;
            /*get { return myModel; }
            set
            {
                myModel = value;
                transforms = new Fusion.Core.Mathematics.Matrix[myModel.Bones.Count];
                for (int i = 0; i < Model.Meshes.Count; i++)
                {
                    for (int j = 0; j < Model.Meshes[i].Effects.Count; j++)
                    {
                        var effect = Model.Meshes[i].Effects[j] as BasicEffect;
                        if (effect != null)
                            effect.EnableDefaultLighting();
                    }
                }
            }*/
        }

        /// <summary>
        /// Gets or sets the texture drawn on this model.
        /// </summary>
        public Texture2D Texture
        {
			get; set;
        }

        /// <summary>
        /// Gets and sets the local transform to apply to the model prior to transforming it by the entity's world matrix.
        /// </summary>
        public Matrix LocalTransform { get; set; }

        /// <summary>
        /// Gets the world transformation applied to the model.
        /// </summary>
        public Matrix WorldTransform { get; private set; }

        /// <summary>
        /// Updates the display object.
        /// </summary>
        public override void Update()
        {
            WorldTransform = LocalTransform * Entity.WorldTransform;
        }

        /// <summary>
        /// Draws the display object.
        /// </summary>
        /// <param name="viewMatrix">Current view matrix.</param>
        /// <param name="projectionMatrix">Current projection matrix.</param>
        public override void Draw(Matrix viewMatrix, Matrix projectionMatrix)
        {
            //This is not a particularly fast method of drawing.
            //It's used very rarely in the demos.
            /*myModel.CopyAbsoluteBoneTransformsTo(transforms);
            for (int i = 0; i < Model.Meshes.Count; i++)
            {
                for (int j = 0; j < Model.Meshes[i].Effects.Count; j++)
                {
                    var effect = Model.Meshes[i].Effects[j] as BasicEffect;
                    if (effect != null)
                    {
                        effect.World = transforms[Model.Meshes[i].ParentBone.Index] * MathConverter.Convert(WorldTransform);
                        effect.View = MathConverter.Convert(viewMatrix);
                        effect.Projection = MathConverter.Convert(projectionMatrix);
                    }
                }
                Model.Meshes[i].Draw();
            } */
        }
    }
}