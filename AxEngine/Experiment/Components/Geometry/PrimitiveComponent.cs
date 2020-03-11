﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Aximo.Render;

namespace Aximo.Engine
{

    public class PrimitiveComponent : SceneComponent
    {

        public bool CastShadow { get; set; }

        public virtual PrimitiveSceneProxy CreateProxy()
        {
            return new PrimitiveSceneProxy(this);
        }

        private List<IMaterialInterface> _Materials;
        public ICollection<IMaterialInterface> Materials { get; private set; }

        public PrimitiveComponent()
        {
            _Materials = new List<IMaterialInterface>();
            Materials = new ReadOnlyCollection<IMaterialInterface>(_Materials);
        }

        public void AddMaterial(IMaterialInterface material)
        {
            _Materials.Add(material);
        }

        public void RemoveMaterial(IMaterialInterface material)
        {
            _Materials.Remove(material);
        }

    }

}
