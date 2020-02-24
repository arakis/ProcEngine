using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace ProcEngine
{

    public class TestObject : SimpleVertexObject
    {

        public override void Init()
        {
            SetTexture("Ressources/woodenbox.png", "Ressources/woodenbox_specular.png");

            if (Debug)
                SetVertices(DataHelper.CubeDebug);
            else
                SetVertices(DataHelper.Cube);

            base.Init();
        }

    }

}