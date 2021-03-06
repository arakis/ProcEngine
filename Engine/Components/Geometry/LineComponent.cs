﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aximo.Render.OpenGL;
using OpenToolkit.Mathematics;

namespace Aximo.Engine.Components.Geometry
{
    public class LineComponent : StaticMeshComponent
    {
        public LineComponent(Vector3 start, Vector3 end)
            : this(start, end, new Vector4(0, 1, 0, 1))
        {
        }

        public LineComponent(Vector3 start, Vector3 end, Vector4 color)
            : this(start, end, color, color)
        {
        }

        public LineComponent(Vector3 start, Vector3 end, Vector4 colorStart, Vector4 colorEnd)
            : base(MeshDataBuilder.Line(start, end, colorStart, colorEnd), MaterialManager.DefaultLineMaterial)
        {
        }
    }
}
