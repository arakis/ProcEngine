// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;

namespace Aximo.Render
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexDataPosUV
    {

        public Vector3 Position;
        public Vector2 UV;

        public VertexDataPosUV(Vector3 position, Vector2 uv)
        {
            Position = position;
            UV = uv;
        }

    }

    public static partial class EngineExtensions
    {
        public static void Add(this IList<VertexDataPosUV> list, Vector3 position, Vector2 uv)
        {
            list.Add(new VertexDataPosUV(position, uv));
        }
    }

}
