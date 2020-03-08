﻿using System;
using System.Collections.Generic;
using OpenToolkit.Graphics.OpenGL4;

namespace AxEngine
{

    public class VertexLayout
    {

        private int _Stride;
        public int Stride => _Stride;

        public void AddAttribute<T>(int index, int size, bool normalized = false)
        {
            var type = typeof(T);
            var offset = _Stride;
            _Stride += size * GetSizeOf(type);
            var attr = new VertexLayoutAttribute
            {
                Index = index,
                Size = size,
                Type = GetVertexAttribPointerType(type),
                Normalized = normalized,
                Stride = 0, // will be set in UpdateStride()
                Offset = offset,
            };
            Attributes.Add(attr);
            UpdateStride();
        }

        private void UpdateStride()
        {
            foreach (var attr in Attributes)
                attr.Stride = _Stride;
        }

        internal void InitAttributes()
        {
            ObjectManager.PushDebugGroup("Init", "VertexLayout");
            foreach (var attr in Attributes)
            {
                if (attr.Index < 0)
                    continue;
                GL.EnableVertexAttribArray(attr.Index);
                GL.VertexAttribPointer(attr.Index, attr.Size, attr.Type, attr.Normalized, attr.Stride, attr.Offset);
            }
            ObjectManager.PopDebugGroup();
        }

        private static VertexAttribPointerType GetVertexAttribPointerType(Type type)
        {
            if (type == typeof(float))
                return VertexAttribPointerType.Float;
            throw new NotImplementedException();
        }

        private static int GetSizeOf(Type type)
        {
            if (type == typeof(float))
                return 4;
            throw new NotImplementedException();
        }

        private List<VertexLayoutAttribute> Attributes = new List<VertexLayoutAttribute>();

    }

}
