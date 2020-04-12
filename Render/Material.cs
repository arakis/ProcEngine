﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using OpenToolkit;
using OpenToolkit.Mathematics;

namespace Aximo.Render
{
    public enum MaterialColorBlendMode
    {
        None = 0,
        Set = 1,
        Multiply = 2,
        Add = 3,
        Sub = 4,
    }

    public class Material
    {

        public Vector3 DiffuseColor { get; set; }

        public float SpecularStrength { get; set; }
        public float Shininess { get; set; }

        public float Ambient { get; set; }

        public bool CastShadow;

        public Shader Shader { get; set; }
        public Shader DefGeometryShader { get; set; }
        public Shader ShadowShader { get; set; }
        public Shader CubeShadowShader { get; set; }

        public Texture DiffuseMap;
        public Texture SpecularMap;

        public IRenderPipeline RenderPipeline;

        public static Material GetDefault()
        {
            var mat = new Material()
            {
                DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f),
                Ambient = 0.3f,
                Shininess = 32.0f,
                SpecularStrength = 0.5f,
            };
            mat.CreateShaders();
            return mat;
        }

        public void CreateShaders()
        {
            if (Shader == null)
                Shader = new Shader("Shaders/forward.vert", "Shaders/forward.frag");
            if (DefGeometryShader == null)
                DefGeometryShader = new Shader("Shaders/deferred-gbuffer.vert", "Shaders/deferred-gbuffer.frag");
            if (ShadowShader == null)
                ShadowShader = new Shader("Shaders/shadow-directional.vert", "Shaders/shadow-directional.frag", "Shaders/shadow-directional.geom");
            if (CubeShadowShader == null)
                CubeShadowShader = new Shader("Shaders/shadow-cube.vert", "Shaders/shadow-cube.frag", "Shaders/shadow-cube.geom");
        }

        public void WriteToShader(string name, Shader shader)
        {
            var prefix = name += ".";
            shader.SetVector3(prefix + "DiffuseColor", DiffuseColor);
            shader.SetInt(prefix + "DiffuseMap", 0);
            shader.SetInt(prefix + "SpecularMap", 1);
            shader.SetFloat(prefix + "Ambient", Ambient);
            shader.SetFloat(prefix + "Shininess", Shininess);
            shader.SetFloat(prefix + "SpecularStrength", SpecularStrength);
        }
    }

}
