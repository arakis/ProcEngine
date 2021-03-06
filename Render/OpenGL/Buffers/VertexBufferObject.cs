﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenToolkit.Graphics.OpenGL4;

namespace Aximo.Render.OpenGL
{
    public class VertexBufferObject : BufferObject
    {
        public VertexBufferObject()
        {
            Target = BufferTarget.ArrayBuffer;
        }
    }
}
