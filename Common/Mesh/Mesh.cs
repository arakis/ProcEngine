﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Aximo.Render;
using Aximo.VertexData;
using Microsoft.VisualBasic.CompilerServices;
using OpenToolkit.Mathematics;
using SixLabors.ImageSharp.Processing;

namespace Aximo
{
    /// <summary>
    /// Represents a Mesh. Can contain vary Polygons.
    /// </summary>
    public partial class Mesh
    {
        internal IList<InternalMeshFace> InternalMeshFaces = new List<InternalMeshFace>();

        /// <summary>
        /// Mixed Face Types (Poly, quad, ...)
        /// </summary>
        internal IList<int> Indicies = new List<int>();
        public MeshFaceType PrimitiveType = MeshFaceType.Triangle;
        public IList<MeshComponent> Components { get; private set; } = new List<MeshComponent>();
        public HashSet<int> MaterialIds { get; private set; } = new HashSet<int>(new int[] { 0 });
        public string Name { get; set; }

        public MeshComponent AddComponent(MeshComponentType componentType)
        {
            var comp = MeshComponent.Create(componentType);
            Components.Add(comp);
            return comp;
        }

        public T AddComponent<T>()
            where T : MeshComponent, new()
        {
            var comp = new T();
            AddComponent(comp);
            return comp;
        }

        public void AddComponent(MeshComponent component)
        {
            Components.Add(component);
        }

        public void AddComponents<T>()
            where T : IVertex
        {
            var type = typeof(T);
            if (typeof(IVertexPosition3).IsAssignableFrom(type))
            {
                AddComponent(new MeshPosition3Component());
            }
            if (typeof(IVertexPosition2).IsAssignableFrom(type))
            {
                AddComponent(new MeshPosition2Component());
            }
            if (typeof(IVertexNormal).IsAssignableFrom(type))
            {
                AddComponent(new MeshNormalComponent());
            }
            if (typeof(IVertexUV).IsAssignableFrom(type))
            {
                AddComponent(new MeshUVComponent());
            }
            if (typeof(IVertexColor).IsAssignableFrom(type))
            {
                AddComponent(new MeshColorComponent());
            }
        }

        // public static void CreateFromVertices<VertexDataPosNormalColor>()
        // {
        // }

        // public static void CreateFromVertices(VertexDataPos2UV[] vertices)
        // {
        // }

        public static Mesh CreateSphere(int divisions = 2)
        {
            var ico = new Util.IcoSphere.IcoSphereMesh(divisions);
            return CreateFromVertices(ico.Vertices, ico.Indicies);
        }

        public static Mesh CreateCube()
        {
            return CreateFromVertices(DataHelper.DefaultCube);
        }

        public static Mesh CreateRoundedCube()
        {
            var builder = new Aximo.Util.RoundedCube.RoundedCubeGenerator
            {
                SizeX = 40,
                SizeY = 40,
                SizeZ = 40,
                Roundness = 15,
            };
            builder.Generate();
            var mesh = builder.Mesh;
            mesh.Scale(1f / 40f);
            mesh.Translate(-0.5f, -0.5f, -0.5f);
            return mesh;
        }

        public static Mesh CreateWallQuad()
        {
            return CreateFromVertices(VertexDataPosNormalUV.WallQuad.ToVertices(), null, MeshFaceType.Quad);
        }

        public static Mesh CreateQuad3()
        {
            return CreateFromVertices(VertexDataPosNormalUV.DefaultQuad.ToVertices(), null, MeshFaceType.Quad);
        }

        public static Mesh CreateQuad2()
        {
            return CreateFromVertices(VertexDataPos2UV.DefaultQuad.ToVertices(), null, MeshFaceType.Quad);
        }

        public static Mesh CreateCylinder(float diameterBottom = 1f, float? diameterTop = null, float height = 1.0f, int slices = 8)
        {
            if (diameterTop == null)
                diameterTop = diameterBottom;

            var path = PathBuilder.Circle(slices).ClosePath().ToVector3();

            var topPath = path.Copy();
            var topPos = new Vector3(0, 0, height / 2f);
            topPath.Scale((float)diameterTop);
            topPath.Translate(topPos);
            var topSurface = CreateSurface(topPath, topPos);

            var bottomPath = path.Copy();
            var bottomPos = new Vector3(0, 0, -height / 2f);
            bottomPath.Scale(diameterBottom);
            bottomPath.Translate(bottomPos);
            var bottomSurface = CreateSurface(bottomPath, bottomPos);

            var mesh = CreateQuadStride(topPath, bottomPath);
            mesh.AddMesh(topSurface);
            return mesh;
        }

        public static Mesh CreateQuadStride(IEnumerable<Vector2> path)
        {
            var vertices = new List<VertexDataPosNormalUV>();
            foreach (var line in path.ToLines())
            {
                var quad = VertexDataPosNormalUV.WallQuad;
                quad.SetLeftRightPosition(line);
                vertices.AddRange(quad.ToVertices());
            }
            return CreateFromVertices(vertices.ToArray(), null, MeshFaceType.Quad);
        }

        public static Mesh CreateQuadStride(IEnumerable<Vector3> topPath, IEnumerable<Vector3> bottomPath)
        {
            var vertices = new List<VertexDataPosNormalUV>();
            var topArray = topPath.ToLines().ToArray();
            var bottomArray = bottomPath.ToLines().ToArray();

            if (topArray.Length != bottomArray.Length)
                throw new NotSupportedException();

            for (var i = 0; i < topArray.Length; i++)
            {
                var quad = VertexDataPosNormalUV.WallQuad;
                quad.SetTopPosition(topArray[i]);
                quad.SetBottomPosition(bottomArray[i]);
                vertices.AddRange(quad.ToVertices());
            }
            return CreateFromVertices(vertices.ToArray(), null, MeshFaceType.Quad);
        }

        public static Mesh CreateSurface(IEnumerable<Vector2> path)
        {
            return CreateSurface(path, path.First());
        }

        public static Mesh CreateSurface(IEnumerable<Vector2> path, Vector2 center)
        {
            var uvOffset = new Vector2(0.5f);
            var uvFactor = new Vector2(-1f, 1);
            var vertices = new List<VertexDataPosNormalUV>();
            foreach (var line in path.ToLines())
            {
                vertices.Add(new VertexDataPosNormalUV(new Vector3(center), Vector3.UnitZ, -new Vector2(0, 0) + uvOffset));
                vertices.Add(new VertexDataPosNormalUV(new Vector3(line.A), Vector3.UnitZ, (-line.A * uvFactor) + uvOffset));
                vertices.Add(new VertexDataPosNormalUV(new Vector3(line.B), Vector3.UnitZ, (-line.B * uvFactor) + uvOffset));
            }
            return CreateFromVertices(vertices.ToArray(), null, MeshFaceType.Triangle);
        }

        public static Mesh CreateSurface(IEnumerable<Vector3> path)
        {
            return CreateSurface(path, path.First());
        }

        public static Mesh CreateSurface(IEnumerable<Vector3> path, Vector3 center)
        {
            var uvOffset = new Vector2(0.5f);
            var uvFactor = new Vector2(-1f, 1);
            var vertices = new List<VertexDataPosNormalUV>();
            foreach (var line in path.ToLines())
            {
                vertices.Add(new VertexDataPosNormalUV(new Vector3(center), Vector3.UnitZ, -new Vector2(0, 0) + uvOffset));
                vertices.Add(new VertexDataPosNormalUV(new Vector3(line.A), Vector3.UnitZ, (-line.A.Xy * uvFactor) + uvOffset));
                vertices.Add(new VertexDataPosNormalUV(new Vector3(line.B), Vector3.UnitZ, (-line.B.Xy * uvFactor) + uvOffset));
            }
            return CreateFromVertices(vertices.ToArray(), null, MeshFaceType.Triangle);
        }

        public static Mesh CreateFromVertices<T>(T[] vertices, int[] indicies = null, MeshFaceType primitiveType = MeshFaceType.Triangle)
            where T : IVertex
        {
            var mesh = new Mesh();
            mesh.PrimitiveType = primitiveType;
            mesh.AddComponents<T>();
            mesh.AddVertices(vertices);

            if (indicies != null)
            {
                var faceCount = indicies.Length / 3;
                for (var faceIndex = 0; faceIndex < faceCount; faceIndex++)
                {
                    var i = faceIndex * 3;
                    mesh.AddFace(indicies[i + 0], indicies[i + 1], indicies[i + 2]);
                }
            }

            return mesh;
        }

        public void ReverseWindingOrder()
        {
            for (var i = 0; i < InternalMeshFaces.Count; i++)
                ReverseWindingOrder(i);
        }

        public void ReverseWindingOrder(int faceIndex)
        {
            var face = InternalMeshFaces[faceIndex];
            Indicies.Reverse(face.StartIndex, face.Count);
        }

        public Mesh Clone()
        {
            var newMesh = CloneEmpty();
            newMesh.PrimitiveType = PrimitiveType;
            var newFace = new List<int>();
            foreach (var comp in Components)
            {
                var destComp = newMesh.GetComponent(comp.Type);
                destComp.AddRange(comp, 0, comp.Count);
            }
            newMesh.Indicies.AddRange(Indicies);
            newMesh.InternalMeshFaces.AddRange(InternalMeshFaces);
            foreach (var matId in MaterialIds)
                newMesh.MaterialIds.Add(matId);
            return newMesh;
        }

        public void Expand()
        {
            ReplaceInternal(Expanded());
        }

        private void ReplaceInternal(Mesh src)
        {
            Components = src.Components;
            InternalMeshFaces = src.InternalMeshFaces;
            Indicies = src.Indicies;
            MaterialIds = src.MaterialIds;
            PrimitiveType = src.PrimitiveType;
        }

        public Mesh Expanded()
        {
            if (Indicies.Count == 0)
                return Clone();

            var newMesh = CloneEmpty();
            newMesh.PrimitiveType = PrimitiveType;
            var newFace = new List<int>();
            foreach (var face in InternalMeshFaces)
            {
                for (var i = 0; i < face.Count; i++)
                {
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[i]]));
                }
                newMesh.AddFace(newFace);
                newFace.Clear();
            }
            foreach (var matId in MaterialIds)
                newMesh.MaterialIds.Add(matId);

            return newMesh;
        }

        public T GetComponent<T>()
            where T : MeshComponent
        {
            foreach (var comp in Components)
                if (comp is T)
                    return (T)comp;
            return null;
        }

        public T GetComponent<T>(MeshComponentType componentType)
            where T : MeshComponent
        {
            foreach (var comp in Components)
                if (comp.Type == componentType && comp is T)
                    return (T)comp;
            return default;
        }

        public MeshComponent GetComponent(MeshComponentType componentType)
        {
            foreach (var comp in Components)
                if (comp.Type == componentType)
                    return comp;
            return null;
        }

        public bool HasComponent<T>()
            where T : MeshComponent
        {
            return GetComponent<T>() != null;
        }

        public bool HasComponent(MeshComponentType componentType)
        {
            return GetComponent(componentType) != null;
        }

        public bool HasComponents<T1, T2>()
            where T1 : MeshComponent
            where T2 : MeshComponent
        {
            return HasComponent<T1>() && HasComponent<T2>();
        }

        public bool HasComponents<T1, T2, T3>()
            where T1 : MeshComponent
            where T2 : MeshComponent
            where T3 : MeshComponent
        {
            return HasComponent<T1>() && HasComponent<T2>() && HasComponent<T3>();
        }

        public bool IsComponents<T1, T2>()
            where T1 : MeshComponent
            where T2 : MeshComponent
        {
            return Components.Count == 2 && HasComponents<T1, T1>();
        }

        public bool IsComponents<T1, T2, T3>()
            where T1 : MeshComponent
            where T2 : MeshComponent
            where T3 : MeshComponent
        {
            return Components.Count == 3 && HasComponents<T1, T1, T3>();
        }

        public void AddFace(params int[] indicies)
        {
            var face = new InternalMeshFace()
            {
                StartIndex = Indicies.Count,
                Count = indicies.Length,
            };
            InternalMeshFaces.Add(face);
            for (var i = 0; i < indicies.Length; i++)
                Indicies.Add(indicies[i]);
        }

        public void AddFace(IList<int> indicies)
        {
            var face = new InternalMeshFace()
            {
                StartIndex = Indicies.Count,
                Count = indicies.Count,
            };
            InternalMeshFaces.Add(face);
            for (var i = 0; i < indicies.Count; i++)
                Indicies.Add(indicies[i]);
        }

        public void AddFaceAndMaterial(int materialId, params int[] indicies)
        {
            var face = new InternalMeshFace()
            {
                StartIndex = Indicies.Count,
                Count = indicies.Length,
                MaterialId = materialId,
            };

            // TODO: Improve. Track every matrialID.
            MaterialIds.Add(materialId);

            InternalMeshFaces.Add(face);
            for (var i = 0; i < indicies.Length; i++)
                Indicies.Add(indicies[i]);
        }

        public void AddFaceFromVerticesTail()
        {
            AddFaceFromVerticesTail(PrimitiveType);
        }

        public void AddFaceFromVerticesTail(MeshFaceType faceType)
        {
            switch (faceType)
            {
                case MeshFaceType.Point:
                    AddFaceFromVerticesPosition(VertexCount - 1, faceType);
                    break;
                case MeshFaceType.Line:
                    AddFaceFromVerticesPosition(VertexCount - 2, faceType);
                    break;
                case MeshFaceType.Triangle:
                    AddFaceFromVerticesPosition(VertexCount - 3, faceType);
                    break;
                case MeshFaceType.Quad:
                    AddFaceFromVerticesPosition(VertexCount - 4, faceType);
                    break;
                default:
                    throw new NotSupportedException(PrimitiveType.ToString());
            }
        }

        public void AddFaceFromVerticesPosition(int vertexStartIndex)
        {
            AddFaceFromVerticesPosition(vertexStartIndex, PrimitiveType);
        }

        public void AddFaceFromVerticesPosition(int vertexStartIndex, MeshFaceType faceType)
        {
            switch (faceType)
            {
                case MeshFaceType.Point:
                    AddFace(vertexStartIndex);
                    break;
                case MeshFaceType.Line:
                    AddFace(vertexStartIndex, vertexStartIndex + 1);
                    break;
                case MeshFaceType.Triangle:
                    AddFace(vertexStartIndex, vertexStartIndex + 1, vertexStartIndex + 2);
                    break;
                case MeshFaceType.Quad:
                    AddFace(vertexStartIndex, vertexStartIndex + 1, vertexStartIndex + 2, vertexStartIndex + 3);
                    break;
                default:
                    throw new NotSupportedException(PrimitiveType.ToString());
            }
        }

        public void CreateFacesFromIndicies()
        {
            CreateFacesFromIndicies(PrimitiveType);
        }

        public void CreateFacesFromIndicies(MeshFaceType type)
        {
            InternalMeshFaces.Clear();
            var n = (int)type;
            var faceCount = Indicies.Count / n;
            for (var i = 0; i < faceCount; i++)
                InternalMeshFaces.Add(new InternalMeshFace()
                {
                    StartIndex = i * n,
                    Count = n,
                    MaterialId = 0,
                });

            MaterialIds.Add(0);
        }

        public void CreateFacesAndIndicies()
        {
            CreateFacesAndIndicies(PrimitiveType);
        }

        public void CreateFacesAndIndicies(MeshFaceType type)
        {
            Indicies.Clear();
            InternalMeshFaces.Clear();
            var n = (int)type;
            var faceCount = VertexCount / n;
            for (var i = 0; i < faceCount; i++)
                AddFaceFromVerticesPosition(i * n, type);
        }

        public void SetMaterial(int faceIndex, int materialId)
        {
            var face = InternalMeshFaces[faceIndex];
            face.MaterialId = materialId;
            InternalMeshFaces[faceIndex] = face;
            MaterialIds.Add(materialId);
        }

        public void ReplaceMaterial(int oldMaterialId, int newMaterialid)
        {
            for (var i = 0; i < InternalMeshFaces.Count; i++)
            {
                var face = InternalMeshFaces[i];
                if (face.MaterialId == oldMaterialId)
                {
                    face.MaterialId = newMaterialid;
                    InternalMeshFaces[i] = face;
                }
            }
            MaterialIds.Remove(oldMaterialId);
            MaterialIds.Add(newMaterialid);
        }

        public void SetNormals(IEnumerable<Vector3> normals)
        {
            var comp = GetComponent<MeshNormalComponent>();
            if (comp == null)
                comp = AddComponent<MeshNormalComponent>();

            comp.Clear();
            comp.AddRange(normals);
        }

        public void RecalculateNormals(float angle)
        {
            NormalSolver.RecalculateNormals(this, angle);
        }

        public void AddMesh(Mesh mesh, int newMaterialId = -1, int filterMaterialId = -1)
        {
            AddMeshInternal(mesh, filterMaterialId, newMaterialId);
        }

        internal void AddMeshInternal(Mesh mesh, int filterMaterialId = -1, int newMaterialId = -1)
        {
            if (FaceCount == 0)
                CreateFacesAndIndicies();

            if (mesh.FaceCount == 0)
                mesh.CreateFacesAndIndicies();

            if (mesh.PrimitiveType > PrimitiveType)
                PrimitiveType = mesh.PrimitiveType;

            var newFace = new List<int>();
            foreach (var face in mesh.InternalMeshFaces)
            {
                if (filterMaterialId > -1 && face.MaterialId != filterMaterialId)
                    continue;

                for (var i = 0; i < face.Count; i++)
                    newFace.Add(AddVertex(mesh, mesh.Indicies[face[i]]));

                if (newMaterialId == -1)
                    AddFace(newFace);
                else
                    AddFaceAndMaterial(newMaterialId, newFace.ToArray());

                newFace.Clear();
            }
        }

        public Mesh CloneEmpty()
        {
            var mesh = new Mesh();

            foreach (var c in Components)
                mesh.AddComponent(c.CloneEmpty());

            mesh.PrimitiveType = PrimitiveType;
            mesh.Name = Name;
            return mesh;
        }

        public void Rotate(Quaternion quaternion)
        {
            var comp = GetComponent<MeshPosition3Component>();
            for (var i = 0; i < comp.Count; i++)
                comp[i] = Vector3.Transform(comp[i], quaternion);
        }

        public void Rotate(Rotor3 quaternion)
        {
            var comp = GetComponent<MeshPosition3Component>();
            for (var i = 0; i < comp.Count; i++)
                comp[i] = Rotor3.Rotate(quaternion, comp[i]);
        }

        public void Scale(float scaleX, float scaleY)
        {
            Scale(new Vector2(scaleX, scaleY));
        }

        public void Scale(float scaleX, float scaleY, float scaleZ)
        {
            Scale(new Vector3(scaleX, scaleY, scaleZ));
        }

        public void Scale(float scale)
        {
            if (HasComponent<MeshPosition3Component>())
                Scale(new Vector3(scale));
            else
                Scale(new Vector2(scale));
        }

        public void Scale(Vector2 scale)
        {
            var comp2 = GetComponent<MeshPosition2Component>();
            if (comp2 != null)
            {
                for (var i = 0; i < comp2.Count; i++)
                    comp2[i] *= scale;
            }
            else
            {
                var comp3 = GetComponent<MeshPosition3Component>();
                for (var i = 0; i < comp3.Count; i++)
                    comp3[i] *= new Vector3(scale.X, scale.Y, 1f);
            }
        }

        public void Scale(Vector3 scale)
        {
            var comp = GetComponent<MeshPosition3Component>();
            for (var i = 0; i < comp.Count; i++)
                comp[i] *= scale;
        }

        public void Translate(float x, float y, float z)
        {
            Translate(new Vector3(x, y, z));
        }

        public void Translate(Vector3 direction)
        {
            var comp = GetComponent<MeshPosition3Component>();
            for (var i = 0; i < comp.Count; i++)
                comp[i] += direction;
        }

        public void TranslateX(float x)
        {
            Translate(new Vector3(x, 0, 0));
        }

        public void TranslateY(float y)
        {
            Translate(new Vector3(0, y, 0));
        }

        public void TranslateZ(float z)
        {
            Translate(new Vector3(0, 0, z));
        }

        public void Translate(float x, float y)
        {
            Translate(new Vector2(x, y));
        }

        public void Translate(Vector2 direction)
        {
            var comp = GetComponent<MeshPosition2Component>();
            for (var i = 0; i < comp.Count; i++)
                comp[i] += direction;
        }

        public int VertexCount => Components == null ? 0 : Components[0].Count;

        public int FaceCount => InternalMeshFaces.Count;

        public Mesh ToPrimitive()
        {
            return ToPrimitive(PrimitiveType, 0);
        }

        public Mesh ToPrimitive(MeshFaceType targetFaceType, int materialId = -1)
        {
            var newMesh = CloneEmpty();
            newMesh.PrimitiveType = targetFaceType;

            if (FaceCount == 0)
                CreateFacesAndIndicies();

            var newFace = new List<int>();
            foreach (var face in InternalMeshFaces)
            {
                if (materialId != -1 && face.MaterialId != materialId)
                    continue;

                if (face.IsPoint && targetFaceType == MeshFaceType.Point)
                {
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[0]]));
                    newMesh.AddFace(newFace);
                    newFace.Clear();
                }
                else if (face.IsLine && targetFaceType == MeshFaceType.Line)
                {
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[0]]));
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[1]]));
                    newMesh.AddFace(newFace);
                    newFace.Clear();
                }
                else if (face.IsTriangle && targetFaceType == MeshFaceType.Triangle)
                {
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[0]]));
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[1]]));
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[2]]));
                    newMesh.AddFace(newFace);
                    newFace.Clear();
                }
                else if (face.IsQuad && (targetFaceType == MeshFaceType.Triangle))
                {
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[0]]));
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[1]]));
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[3]]));
                    newMesh.AddFace(newFace);
                    newFace.Clear();

                    newFace.Add(newMesh.AddVertex(this, Indicies[face[2]]));
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[3]]));
                    newFace.Add(newMesh.AddVertex(this, Indicies[face[1]]));
                    newMesh.AddFace(newFace);
                    newFace.Clear();
                }
                else
                {
                    throw new NotSupportedException(face.Type.ToString());
                }
            }
            return newMesh;
        }

        public IList<MeshFace<T>> FaceView<T>()
            where T : IVertex
        {
            return new MeshFaceList<T>(this);
        }

        public int AddVertex(Mesh src, int index)
        {
            AddVertices(src, index, 1);
            return VertexCount - 1;
        }

        public void AddVertices(Mesh src, int start, int count)
        {
            foreach (var c in Components)
            {
                var srcComp = src.GetComponent(c.Type);
                if (srcComp == null)
                    continue;

                c.AddRange(srcComp, start, count);
            }
        }

        public void AddVertices<T>(ICollection<T> values)
            where T : IVertex
        {
            var type = typeof(T);
            if (type.IsInterface)
            {
                View<T>().AddRange(values);
                return;
            }

            if (typeof(IVertexPosition3).IsAssignableFrom(type))
                GetComponent<MeshPosition3Component>()?.AddRange(values);
            if (typeof(IVertexPosition2).IsAssignableFrom(type))
                GetComponent<MeshPosition2Component>()?.AddRange(values);
            if (typeof(IVertexNormal).IsAssignableFrom(type))
                GetComponent<MeshNormalComponent>()?.AddRange(values);
            if (typeof(IVertexUV).IsAssignableFrom(type))
                GetComponent<MeshUVComponent>()?.AddRange(values);
            if (typeof(IVertexColor).IsAssignableFrom(type))
                GetComponent<MeshColorComponent>()?.AddRange(values);
        }

        public void Visit<T>(Action<T> callback)
            where T : IVertex
        {
            var enu = new MeshVertexEnumerator<T>(this);
            while (enu.MoveNext())
                callback(enu.Current);
        }

        public void Visit<T>(Action<T, int> callback)
            where T : IVertex
        {
            var enu = new MeshVertexEnumerator<T>(this);
            while (enu.MoveNext())
                callback(enu.Current, enu.Visitor.Index);
        }

        public MeshVertexList<T> View<T>()
            where T : IVertex
        {
            return new MeshVertexList<T>(VertexVisitor<T>.CreateVisitor(this));
        }

        public T[] GetVertexArray<T>()
        {
            if (typeof(T) == typeof(VertexDataPosNormalUV))
                return View<IVertexPosNormalUV>().ToArray<T>();
            if (typeof(T) == typeof(VertexDataPosNormalColor))
                return View<IVertexPosNormalColor>().ToArray<T>();
            if (typeof(T) == typeof(VertexDataPos2UV))
                return View<IVertexPos2UV>().ToArray<T>();

            throw new NotSupportedException(typeof(T).Name);
        }

        public int[] GetIndiciesArray()
        {
            return Indicies.ToArray();
        }

        public int[] GetIndiciesArray(int materialId)
        {
            var indicies = new List<int>();
            foreach (var face in InternalMeshFaces)
            {
                if (face.MaterialId == materialId)
                    for (var i = 0; i < face.Count; i++)
                        indicies.Add(Indicies[face[i]]);
            }
            return indicies.ToArray();
        }

        public T[] GetIndiciesArray<T>()
            where T : unmanaged
        {
            return GetIndiciesArray().Select(v => (T)Convert.ChangeType(v, typeof(T))).ToArray();
        }

        public BufferData1D<T> GetIndiciesBuffer<T>()
            where T : unmanaged
        {
            if (Indicies.Count == 0)
                return null;
            return new BufferData1D<T>(GetIndiciesArray<T>());
        }

        public bool IsCompatible<T>()
        where T : IVertex
        {
            if (typeof(T) == typeof(IVertexPosNormalUV))
                return IsComponents<MeshPosition3Component, MeshNormalComponent, MeshUVComponent>();
            if (typeof(T) == typeof(IVertexPosNormalColor))
                return IsComponents<MeshPosition3Component, MeshNormalComponent, MeshColorComponent>();
            if (typeof(T) == typeof(IVertexPosColor))
                return IsComponents<MeshPosition3Component, MeshColorComponent>();
            if (typeof(T) == typeof(IVertexPos2UV))
                return IsComponents<MeshPosition2Component, MeshUVComponent>();

            return false;
        }

        public AxPrimitiveType GetPrimitiveType()
        {
            switch (PrimitiveType)
            {
                case MeshFaceType.Line:
                    return AxPrimitiveType.Lines;
                case MeshFaceType.Triangle:
                    return AxPrimitiveType.Triangles;
                default:
                    throw new NotSupportedException(PrimitiveType.ToString());
            }
        }

        public int MaterialCount => MaterialIds.Count;

        public Box3 Bounds
        {
            get
            {
                var comp = GetComponent<MeshPosition3Component>();
                if (comp != null)
                    return comp.Bounds;

                var comp2 = GetComponent<MeshPosition2Component>();
                if (comp2 != null)
                {
                    var box = comp2.Bounds;
                    return new Box3(new Vector3(box.Min), new Vector3(box.Max));
                }

                return default;
            }
        }

        public void CalculateBounds()
        {
            var comp = GetComponent<MeshPosition3Component>();
            if (comp != null)
                comp.CalculateBounds();
            else
                GetComponent<MeshPosition2Component>()?.CalculateBounds();
        }

        public override string ToString()
        {
            return $"Vertices={VertexCount} Indicies={Indicies.Count} Faces={FaceCount} Type={PrimitiveType} Materials={MaterialCount}";
        }
    }
}
