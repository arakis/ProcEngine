﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aximo.Render;
using Aximo.Render.OpenGL;
using OpenToolkit.Mathematics;
using SixLabors.ImageSharp;

#pragma warning disable CA1044 // Properties should not be write only

namespace Aximo.Engine.Components.Geometry
{
    public class ScreenTextureComponent : StaticMeshComponent
    {
        public ScreenTextureComponent() : base(MeshDataBuilder.NDCQuadInvertedUV(), MaterialManager.CreateScreenMaterial())
        {
        }

        public ScreenTextureComponent(string texturePath) : base(MeshDataBuilder.NDCQuadInvertedUV(), MaterialManager.CreateScreenMaterial(texturePath))
        {
        }

        internal override void SyncChanges()
        {
            if (!HasChanges)
                return;

            base.SyncChanges();

            if (OrderChanged)
            {
                RenderableObject.DrawPriority = Order;
                OrderChanged = false;
            }
        }

        private bool OrderChanged = false;
        private int Order = 5000;

        private int _CustomOrder;
        public int CustomOrder
        {
            get
            {
                return _CustomOrder;
            }
            set
            {
                if (_CustomOrder == value)
                    return;
                _CustomOrder = value;
                SetOrders();
            }
        }

        internal void SetOrders()
        {
            SetOrders(Order * (_CustomOrder + 1));
        }

        internal void SetOrders(int order)
        {
            Visit<ScreenTextureComponent>(c =>
            {
                c.Order = order++;
                c.OrderChanged = true;
                c.PropertyChanged();
            });
        }

        protected override void OnAttached()
        {
            if (Parent is ScreenTextureComponent s)
                s.SetOrders();
            else
                SetOrders();
        }
    }

}
