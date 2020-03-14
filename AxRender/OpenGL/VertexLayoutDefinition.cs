﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Aximo.Render
{

    public class VertexLayoutDefinition
    {

        public VertexLayout BindToShader(Shader shader)
        {
            var layout = new VertexLayout();
            layout._Stride = _Stride;
            foreach (var srcAttr in Attributes)
            {
                var attr = layout.CreateAttributeInstance() as VertexLayoutAttribute;
                srcAttr.CopyTo(attr);
                attr.Index = shader.GetAttribLocation(attr.Name);
                layout.AddAttribute(attr);
            }
            return layout;
        }

        private List<VertexLayoutDefinitionAttribute> _Attributes;
        public ReadOnlyCollection<VertexLayoutDefinitionAttribute> Attributes { get; private set; }

        public VertexLayoutDefinition()
        {
            _Attributes = new List<VertexLayoutDefinitionAttribute>();
            Attributes = new ReadOnlyCollection<VertexLayoutDefinitionAttribute>(_Attributes);
        }

        protected virtual void AddAttribute(VertexLayoutDefinitionAttribute attr)
        {
            _Attributes.Add(attr);
        }

        private int _Stride;
        public int Stride => _Stride;

        protected virtual VertexLayoutDefinitionAttribute CreateAttributeInstance()
        {
            return new VertexLayoutDefinitionAttribute();
        }

        public virtual VertexLayoutDefinitionAttribute AddAttribute<T>(string name, bool normalized = false)
        {
            return AddAttribute<T>(name, StructHelper.GetFieldsOf<T>(), normalized);
        }

        public virtual VertexLayoutDefinitionAttribute AddAttribute<T>(string name, int size, bool normalized = false)
        {
            var offset = _Stride;
            _Stride += size * StructHelper.GetFieldSizeOf<T>();

            var attr = CreateAttributeInstance();
            attr.Name = name;
            attr.Size = size;
            attr.Type = StructHelper.GetVertexAttribPointerType<T>();
            attr.Normalized = normalized;
            attr.Stride = 0; // will be set in UpdateStride()
            attr.Offset = offset;

            AddAttribute(attr);
            UpdateStride();
            return attr;
        }

        private void UpdateStride()
        {
            foreach (var attr in Attributes)
                attr.Stride = _Stride;
        }

        public void DumpDebug()
        {
            Console.WriteLine($"Dump of {GetType().Name}. Stride: {Stride}");
            foreach (var attr in Attributes)
                Console.WriteLine(attr.GetDumpString());
        }

    }

}
