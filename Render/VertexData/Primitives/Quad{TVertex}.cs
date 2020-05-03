﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenToolkit.Mathematics;

namespace Aximo.Render
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Quad<TVertex> : IPrimitive<TVertex>
    {
        public TVertex Vertex0;
        public TVertex Vertex1;
        public TVertex Vertex2;
        public TVertex Vertex3;

        public Quad(TVertex[] vertices)
        {
            Vertex0 = vertices[0];
            Vertex1 = vertices[1];
            Vertex2 = vertices[2];
            Vertex3 = vertices[3];
        }

        public Quad(TVertex vertex0, TVertex vertex1, TVertex vertex2, TVertex vertex3)
        {
            Vertex0 = vertex0;
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex3 = vertex3;
        }

        public TVertex this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Vertex0;
                    case 1:
                        return Vertex1;
                    case 2:
                        return Vertex2;
                    case 3:
                        return Vertex3;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Vertex0 = value;
                        return;
                    case 1:
                        Vertex1 = value;
                        return;
                    case 2:
                        Vertex2 = value;
                        return;
                    case 3:
                        Vertex3 = value;
                        return;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public int Count => 4;

        public void CopyTo(ICollection<IPrimitive<TVertex>> destination)
        {
            for (var i = 0; i < Count; i++)
                destination.Add((IPrimitive<TVertex>)this[i]);
        }

        public void CopyTo(ICollection<IPrimitive> destination)
        {
            for (var i = 0; i < Count; i++)
                destination.Add((IPrimitive)this[i]);
        }

        public Polygon<TVertex>[] ToPolygons()
        {
            // Vertex order:
            // -------------
            // 3  2       3        3  2
            //       -->        +
            // 0  1       0  1        1
            // -------    -----    -----
            // 0,1,2,3 -> 0,1,3    2,3,1

            var p1 = new Polygon<TVertex>();
            p1.Vertex0 = Vertex0;
            p1.Vertex1 = Vertex1;
            p1.Vertex2 = Vertex3;

            var p2 = new Polygon<TVertex>();
            p1.Vertex0 = Vertex2;
            p1.Vertex1 = Vertex3;
            p1.Vertex2 = Vertex1;

            return new Polygon<TVertex>[] { p1, p2 };
        }

        public TVertex[] ToVertices()
        {
            return new TVertex[] { Vertex0, Vertex1, Vertex2, Vertex3 };
        }

        public TVertex[] ToPolygonVertices()
        {
            return new TVertex[] { Vertex0, Vertex1, Vertex3, Vertex2, Vertex3, Vertex1 };
        }
    }

    public static class QuadExtensions
    {
        public static void Rotate(this ref Quad<VertexDataPos> quad, Rotor3 q)
        {
            for (var i = 0; i < quad.Count; i++)
            {
                var v = quad[i];
                v.Position = Rotor3.Rotate(q, v.Position);
                quad[i] = v;
            }
        }

        public static void Rotate(this ref Quad<VertexDataPosNormalUV> quad, Rotor3 q)
        {
            for (var i = 0; i < quad.Count; i++)
            {
                var v = quad[i];
                v.Position = Rotor3.Rotate(q, v.Position);
                v.Normal = Rotor3.Rotate(q, v.Normal);
                quad[i] = v;
            }
        }

        public static void Round(this ref Quad<VertexDataPos> quad, int digits)
        {
            for (var i = 0; i < quad.Count; i++)
            {
                var v = quad[i];
                v.Position = v.Position.Round(digits);
                quad[i] = v;
            }
        }

        public static void RoundSmooth(this ref Quad<VertexDataPos> quad)
        {
            Round(ref quad, 6);
        }

        public static void Round(this ref Quad<VertexDataPosNormalUV> quad, int digits)
        {
            for (var i = 0; i < quad.Count; i++)
            {
                var v = quad[i];
                v.Normal = v.Normal.Round(digits);
                v.Position = v.Position.Round(digits);
                quad[i] = v;
            }
        }

        public static void RoundSmooth(this ref Quad<VertexDataPosNormalUV> quad)
        {
            Round(ref quad, 6);
        }
    }

}
