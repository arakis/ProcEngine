﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenToolkit.Mathematics;

namespace Aximo.VertexData
{
    public interface IVertexColor : IVertex
    {
        Vector4 Color { get; set; }

        new IVertexColor Clone();
    }
}
