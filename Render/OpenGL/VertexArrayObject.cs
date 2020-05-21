﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using OpenToolkit.Graphics.OpenGL4;

namespace Aximo.Render.OpenGL
{
    public class VertexArrayObject
    {
        private int _Handle = -1;
        public int Handle => _Handle;

        public int VertexCount { get; private set; }

        public VertexLayoutBinded Layout { get; set; }

        private VertexBufferObject _vbo;
        public VertexBufferObject Vbo => _vbo;

        private ElementsBufferObject _ebo;
        public ElementsBufferObject Ebo => _ebo;

        public void SetVbo(VertexBufferObject vbo)
        {
            _vbo = vbo;
        }

        public void SetEbo(ElementsBufferObject ebo)
        {
            _ebo = ebo;
        }

        public VertexArrayObject(VertexLayoutBinded layout, VertexBufferObject vbo = null, ElementsBufferObject ebo = null)
        {
            Layout = layout;
            _vbo = vbo;
            _ebo = ebo;
        }

        public VertexBufferObject CreateVBO()
        {
            _vbo = new VertexBufferObject();
            _vbo.Create();
            _vbo.Bind();
            return _vbo;
        }

        public ElementsBufferObject CreateEBO()
        {
            _ebo = new ElementsBufferObject();
            _ebo.Create();
            _ebo.Bind();
            return _ebo;
        }

        public void Create()
        {
            _Handle = GL.GenVertexArray();
            Bind();
            // if (Layout != null)
            //     Layout.InitAttributes();
        }

        public static int CurrentHandle;
        public void Bind()
        {
            if (CurrentHandle == _Handle)
                return;
            CurrentHandle = _Handle;

            GL.BindVertexArray(_Handle);
        }

        public void BindDefault()
        {
            CurrentHandle = 0;
            GL.BindVertexArray(0);
        }

        public PrimitiveType PrimitiveType = PrimitiveType.Triangles;

        public void Draw()
        {
            if (_ebo == null)
                GL.DrawArrays(PrimitiveType, 0, VertexCount);
            else
                GL.DrawElements(PrimitiveType, _ebo.Size, DrawElementsType.UnsignedShort, 0);

            if (Renderer.Current.FlushRenderBackend == FlushRenderBackend.Draw)
                GL.Finish();
        }

        private bool Initialized;
        private void EnsureInitialized()
        {
            if (Initialized)
                return;
            if (_Handle == -1)
                Create();
            Bind();
            if (_vbo == null)
                _vbo = CreateVBO();
            // if (_ebo == null)
            //     _ebo = CreateEBO();
            Layout.InitAttributes();
            Initialized = true;
        }

        internal void SetData(BufferData1D vertices, BufferData1D<ushort> indicies = null)
        {
            EnsureInitialized();
            _vbo.SetData(vertices);
            VertexCount = vertices.Length * vertices.ElementSize / Layout.Stride;
            //          UseDefault();
            if (indicies != null)
            {
                if (_ebo == null)
                    _ebo = CreateEBO();
                _ebo.SetData(indicies);
            }
        }

        internal void SetData(MeshData data)
        {
            SetData(data.Data, data.Indicies);
            PrimitiveType = GetPrimitiveType(data.PrimitiveType);
        }

        private PrimitiveType GetPrimitiveType(AxPrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case AxPrimitiveType.Triangles:
                    return PrimitiveType.Triangles;
                case AxPrimitiveType.Lines:
                    return PrimitiveType.Lines;
                default:
                    throw new InvalidOperationException();
            }
        }

        //public void AddPosition()
        //{
        //}

        public void Free()
        {
            GL.DeleteVertexArray(_Handle);
        }
    }
}
