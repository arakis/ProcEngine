﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aximo.VertexData
{
    public interface IVertexPosition<TVector> : IVertex
        where TVector : unmanaged
    {
        TVector Position { get; set; }
        new IVertexPosition<TVector> Clone();
    }
}
