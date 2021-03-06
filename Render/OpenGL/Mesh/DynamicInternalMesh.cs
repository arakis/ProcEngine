﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aximo.Render.OpenGL
{
    public class DynamicInternalMesh : InternalMesh
    {
        public DynamicInternalMesh() : base() { }
        public DynamicInternalMesh(Mesh meshData) : base(meshData) { }
        public DynamicInternalMesh(Mesh meshData, RendererMaterial material) : base(meshData, material) { }
    }
}
