﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aximo.VertexData
{
    public interface IVertexPosNormalUV : IVertexPosition3, IVertexNormal, IVertexUV
    {
        void Set(IVertexPosNormalUV source);
        void Set(VertexDataPosNormalUV source);

        new VertexDataPosNormalUV Clone();
    }
}
