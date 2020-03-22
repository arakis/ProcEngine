﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using Aximo.Render;
using OpenTK;

namespace Aximo.Engine
{

    public class StreamableRenderAsset
    {

        public Stream Stream { get; private set; }

    }

    public class MeshComponent : PrimitiveComponent
    {
    }

    public class CubeComponent : StaticMeshComponent
    {
        public CubeComponent() : base(MeshDataBuilder.Cube(), GameMaterial.Default)
        {
        }
    }

    public class GridPlaneComponent : StaticMeshComponent
    {
        public GridPlaneComponent(int size, bool center) : base(MeshDataBuilder.Grid(size, center), MaterialManager.DefaultLineMaterial)
        {
        }
    }

    public class CrossLineComponent : StaticMeshComponent
    {
        public CrossLineComponent(int size, bool center) : base(MeshDataBuilder.CrossLine(), MaterialManager.DefaultLineMaterial)
        {
        }
    }

    public class DebugCubeComponent : StaticMeshComponent
    {
        public DebugCubeComponent() : base(MeshDataBuilder.DebugCube(), GameMaterial.Default)
        {
        }
    }

    public class StatsComponent : GraphicsScreenTextureComponent
    {
        private DateTime LastStatUpdate;
        private Font DefaultFont = new Font(FontFamily.GenericSansSerif, 15, GraphicsUnit.Point);

        public StatsComponent() : base(100, 100)
        {
        }

        public StatsComponent(int width, int height) : base(width, height)
        {
            Graphics.Clear(Color.Green);
            UpdateTexture();
        }

        public override void UpdateFrame()
        {
            if ((DateTime.UtcNow - LastStatUpdate).TotalSeconds > 1)
            {
                LastStatUpdate = DateTime.UtcNow;
                Graphics.Clear(Color.Transparent);
                var txt = "FPS: " + Math.Round(RenderApplication.Current.RenderFrequency).ToString();
                txt += "\nUPS: " + Math.Round(RenderApplication.Current.UpdateFrequency).ToString();
                Graphics.DrawString(txt, DefaultFont, Brushes.White, new PointF(5, 5));
                UpdateTexture();
            }
        }

    }

    public class GraphicsScreenTextureComponent : ScreenTextureComponent
    {
        public GraphicsScreenTextureComponent(int width, int height)
        {
            Image = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics = Graphics.FromImage(Image);
            Texture = GameTexture.GetFromBitmap(Image, null);
            Material.DiffuseTexture = Texture;
            UpdateTexture();
        }

        private Bitmap Image;

        public Graphics Graphics { get; private set; }

        public GameTexture Texture { get; private set; }

        public void UpdateTexture()
        {
            // Graphics.Save();
            Graphics.Flush();
            // Graphics.Dispose();
            Texture.SetData(Image);
            Update();
        }

    }

    public class ScreenTextureComponent : StaticMeshComponent
    {
        public ScreenTextureComponent() : base(MeshDataBuilder.Quad(), MaterialManager.CreateScreenMaterial())
        {
        }

        public ScreenTextureComponent(string texturePath) : base(MeshDataBuilder.Quad(), MaterialManager.CreateScreenMaterial(texturePath))
        {
        }

        internal override void SyncChanges()
        {
            base.SyncChanges();
        }

        public RectangleF RectangleUV
        {
            set
            {
                var pos = new Vector3(
                    ((value.X + (value.Width / 2f)) * 2) - 1.0f,
                    ((1 - (value.Y + (value.Height / 2f))) * 2) - 1.0f,
                    0);

                var scale = new Vector3(value.Width, -value.Height, 1.0f);
                RelativeTranslation = pos;
                RelativeScale = scale;
            }
        }

        public RectangleF RectanglePixels
        {
            set
            {
                var pos1 = new Vector2(value.X, value.Y) * RenderContext.Current.PixelToUVFactor;
                var pos2 = new Vector2(value.Right, value.Bottom) * RenderContext.Current.PixelToUVFactor;

                RectangleUV = new RectangleF(pos1.X, pos1.Y, pos2.X - pos1.X, pos2.Y - pos1.Y);
            }
        }

    }

    public class SphereComponent : StaticMeshComponent
    {
        public SphereComponent(int divisions = 2) : base(MeshDataBuilder.Sphere(2), GameMaterial.Default)
        {
        }
    }

    public class StaticMeshComponent : MeshComponent
    {

        public StaticMeshComponent()
        {

        }

        public StaticMeshComponent(MeshData mesh)
        {
            SetMesh(mesh);
        }

        public StaticMeshComponent(MeshData mesh, GameMaterial material)
        {
            SetMesh(mesh);
            Material = material;
        }

        public MeshData Mesh { get; private set; }
        private StaticMesh InternalMesh;

        public void SetMesh(MeshData mesh)
        {
            Mesh = mesh;
            UpdateMesh();
        }

        protected internal bool MeshChanged;
        protected internal void UpdateMesh()
        {
            MeshChanged = true;
            Update();
        }

        public override PrimitiveSceneProxy CreateProxy()
        {
            return new StaticMeshSceneProxy(this);
        }

        internal override void SyncChanges()
        {
            if (!HasChanges)
                return;

            base.SyncChanges();

            bool created = false;
            if (RenderableObject == null)
            {
                created = true;
                RenderableObject = new SimpleVertexObject();
            }

            var obj = (SimpleVertexObject)RenderableObject;
            if (MeshChanged)
            {
                if (InternalMesh == null)
                {
                    InternalMesh = new StaticMesh(Mesh);
                    obj.Name = Name;
                }
                InternalMesh.Materials.Clear();
                foreach (var gameMat in Materials)
                {
                    InternalMesh.Materials.Add(gameMat.InternalMaterial);
                }

                obj.SetVertices(InternalMesh);
                MeshChanged = false;
            }

            if (TransformChanged)
            {
                var trans = LocalToWorld();
                obj.PositionMatrix = trans;
                TransformChanged = false;
            }

            if (created)
                RenderContext.Current.AddObject(RenderableObject);
        }

    }

    public class SkyBoxComponent : MeshComponent
    {

        public SkyBoxComponent()
        {

        }

        public SkyBoxComponent(string skyBoxPath)
        {
        }

        internal override void SyncChanges()
        {
            if (!HasChanges)
                return;

            base.SyncChanges();

            bool created = false;
            if (RenderableObject == null)
            {
                created = true;
                RenderableObject = new SkyboxObject();
            }

            var obj = (SkyboxObject)RenderableObject;

            if (created)
                RenderContext.Current.AddObject(RenderableObject);
        }

    }

    public class PrimitiveSceneProxy
    {

        public List<Material> Materials = new List<Material>();

        public PrimitiveSceneProxy(PrimitiveComponent component) { }

    }

    public class StaticMeshSceneProxy : PrimitiveSceneProxy
    {
        public StaticMeshSceneProxy(StaticMeshComponent component) : base(component) { }

        public void DrawStaticElements(StaticPrimitiveDrawInterface pdi)
        {
        }

    }

}
