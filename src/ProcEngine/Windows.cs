﻿using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using ProcEngine;
using System.IO;
using System.Collections.Generic;

namespace ProcEngine
{
    // In this tutorial we set up some basic lighting and look at how the phong model works
    // For more insight into how it all works look at the web version. If you are just here for the source,
    // most of the changes are in the shaders, specifically most of the changes are in the fragment shader as this is
    // where the lighting calculations happens.
    public class Window : GameWindow
    {

        private float[] MouseSpeed = new float[3];
        private Vector2 MouseDelta;
        private float UpDownDelta;

        //public static Matrix4 CameraMatrix;
        //private float[] MouseSpeed = new float[3];
        //private Vector2 MouseDelta;
        //private float UpDownDelta;
        //private Vector3 CameraLocation;
        //private Vector3 Up = Vector3.UnitZ;
        //private float Pitch = -0.3f;
        //private float Facing = (float)Math.PI / 2 + 0.15f;

        public Window(int width, int height, string title) : base(width, height, GraphicsMode.Default, title, GameWindowFlags.Default, DisplayDevice.Default, 4, 0, GraphicsContextFlags.Default) { }

        public Camera Camera => ctx.Camera;

        private double CamAngle = 0;

        protected override void OnLoad(EventArgs e)
        {
            var vendor = GL.GetString(StringName.Vendor);
            var version = GL.GetString(StringName.Version);
            var shadingLanguageVersion = GL.GetString(StringName.ShadingLanguageVersion);
            var renderer = GL.GetString(StringName.Renderer);

            Console.WriteLine($"Vendor: {vendor}, version: {version}, shadinglangVersion: {shadingLanguageVersion}, renderer: {renderer}");

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            ctx = new RenderContext();
            ctx.SceneOpitons = new SceneOptions
            {

            };
            ctx.Camera = new PerspectiveFieldOfViewCamera(new Vector3(1f, -5f, 2f), Width / (float)Height)
            {
                NearPlane = 0.01f,
                FarPlane = 100.0f,
            };
            //ctx.Camera = new PerspectiveFieldOfViewCamera(lightPosition, Width / (float)Height);
            //ctx.Camera = new OrthographicCamera(lightPosition);

            ctx.AddObject(new LightObject()
            {
                Position = new Vector3(0, 2, 2),
            });

            ctx.AddObject(new LightObject()
            {
                Position = new Vector3(1f, 0.5f, 0.5f),
            });

            ctx.AddObject(new TestObject()
            {
                Name = "Box1",
                ModelMatrix = Matrix4.CreateScale(1) * Matrix4.CreateRotationZ((float)Math.PI) * Matrix4.CreateTranslation(0, 0, 0),
                Debug = true,
            });

            ctx.AddObject(new TestObject()
            {
                Name = "Box2",
                ModelMatrix = Matrix4.CreateTranslation(1.5f, 1.5f, 0.0f),
                //Debug = true,
            });

            ctx.AddObject(new Lines()
            {
                Name = "CenterCross",
                ModelMatrix = Matrix4.CreateTranslation(0f, 0f, 0.0f),
                //Debug = true,
            });

            ctx.AddObject(new TestObject()
            {
                Name = "Ground",
                ModelMatrix = Matrix4.CreateScale(8, 8, 8) * Matrix4.CreateTranslation(0f, 0f, -4.5f),
            });

            //CursorVisible = false;

            fb = new FrameBuffer(Width, Height);
            fb.InitNormal();
            fb.CreateRenderBuffer();

            shadowFb = new FrameBuffer(1024, 1024);
            shadowFb.InitDepth();

            shadowCubeFb = new FrameBuffer(1024, 1024);
            shadowCubeFb.InitCubeDepth();

            ctx.AddObject(new ScreenObject(fb.DestinationTexture)
            {
            });

            StartFileListener();

            base.OnLoad(e);
        }

        private FileSystemWatcher ShaderWatcher;

        private void StartFileListener()
        {
            ShaderWatcher = new FileSystemWatcher(Path.Combine(DirectoryHelper.RootDir, "Shaders"));
            ShaderWatcher.Changed += (sender, e) =>
            {
                // Reload have to be in Main-Thread.
                Dispatch(() => Reload());
            };
            ShaderWatcher.EnableRaisingEvents = true;
        }

        private FrameBuffer fb;
        public static FrameBuffer shadowFb;
        public static FrameBuffer shadowCubeFb;

        private RenderContext ctx;

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            CamAngle -= 0.01;
            var pos = new Vector3((float)(Math.Cos(CamAngle) * 2f), (float)(Math.Sin(CamAngle) * 2f), 1.0f);
            ILightObject light = ctx.LightObjects[0];

            light.Position = pos;

            GL.Enable(EnableCap.DepthTest);

            //--

            GL.Viewport(0, 0, shadowFb.Width, shadowFb.Height);
            shadowFb.Use();
            GL.Clear(ClearBufferMask.DepthBufferBit);

            // Render objects
            foreach (var obj in ctx.ShadowObjects)
                obj.OnRenderShadow();

            shadowCubeFb.Use();
            GL.Clear(ClearBufferMask.DepthBufferBit);
            foreach (var obj in ctx.ShadowObjects)
                obj.OnRenderCubeShadow();

            //--

            // Configure
            GL.Viewport(0, 0, Width, Height);
            fb.Use();
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Render objects
            foreach (var obj in ctx.RenderableObjects)
                obj.OnRender();

            // Render Screen Surface
            foreach (var obj in ctx.RenderableScreenObjects)
                obj.OnRender();

            //CheckForProgramError();

            // Commit result
            SwapBuffers();

            base.OnRenderFrame(e);
        }

        private void CheckForProgramError()
        {
            var err = LastErrorCode;
            if (err != ErrorCode.NoError)
            {
                var s = "".ToString();
            }
        }

        public static ErrorCode LastErrorCode => GL.GetError();

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            ProcessTaskQueue();

            if (!Focused)
            {
                return;
            }

            var input = Keyboard.GetState();

            if (input.IsKeyDown(Key.Escape))
            {
                Exit();
                Environment.Exit(0);
            }

            var kbState = Keyboard.GetState();
            if (kbState[Key.W])
            {
                Camera.Position.X += (float)Math.Cos(Camera.Facing) * 0.1f;
                Camera.Position.Y += (float)Math.Sin(Camera.Facing) * 0.1f;
            }

            if (kbState[Key.S])
            {
                Camera.Position.X -= (float)Math.Cos(Camera.Facing) * 0.1f;
                Camera.Position.Y -= (float)Math.Sin(Camera.Facing) * 0.1f;
            }

            if (kbState[Key.A])
            {
                Camera.Position.X += (float)Math.Cos(Camera.Facing + Math.PI / 2) * 0.1f;
                Camera.Position.Y += (float)Math.Sin(Camera.Facing + Math.PI / 2) * 0.1f;
            }

            if (kbState[Key.D])
            {
                Camera.Position.X -= (float)Math.Cos(Camera.Facing + Math.PI / 2) * 0.1f;
                Camera.Position.Y -= (float)Math.Sin(Camera.Facing + Math.PI / 2) * 0.1f;
            }

            if (kbState[Key.Left])
                MouseDelta.X = -2;

            if (kbState[Key.Right])
                MouseDelta.X = 2;

            if (kbState[Key.Up])
                MouseDelta.Y = -1;

            if (kbState[Key.Down])
                MouseDelta.Y = 1;

            if (kbState[Key.PageUp])
                UpDownDelta = -3;

            if (kbState[Key.PageDown])
                UpDownDelta = 3;

            MouseSpeed[0] *= 0.9f;
            MouseSpeed[1] *= 0.9f;
            MouseSpeed[2] *= 0.9f;
            MouseSpeed[0] -= MouseDelta.X / 1000f;
            MouseSpeed[1] -= MouseDelta.Y / 1000f;
            MouseSpeed[2] -= UpDownDelta / 1000f;
            MouseDelta = new Vector2();
            UpDownDelta = 0;

            Camera.Facing += MouseSpeed[0] * 2;
            Camera.Pitch += MouseSpeed[1] * 2;
            //Console.WriteLine(Camera.Pitch + " : " + Math.Round(MouseSpeed[1], 3));
            Camera.Position.Z += MouseSpeed[2] * 2;

            if (kbState[Key.Escape])
                Exit();

            if (kbState[Key.F11])
            {
                Reload();
            }

            if (kbState[Key.F12])
            {
                shadowFb.DestinationTexture.GetDepthTexture().Save("test.png");
            }

            base.OnUpdateFrame(e);
        }

        private Queue<Action> TaskQueue = new Queue<Action>();

        public void Dispatch(Action act)
        {
            lock (TaskQueue)
                TaskQueue.Enqueue(act);
        }

        private void ProcessTaskQueue()
        {
            while (TaskQueue.Count > 0)
            {
                Action act;
                lock (TaskQueue)
                    act = TaskQueue.Dequeue();
                if (act != null)
                    act();
            }
        }

        private void Reload()
        {
            foreach (var obj in ctx.AllObjects)
                if (obj is IReloadable reloadable)
                    reloadable.OnReload();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (e.Mouse.LeftButton == ButtonState.Pressed)
                MouseDelta = new Vector2(e.XDelta, e.YDelta);

            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            Camera.Fov -= e.DeltaPrecise;
            base.OnMouseWheel(e);
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            Camera.AspectRatio = Width / (float)Height;
            base.OnResize(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

            foreach (var obj in ctx.AllObjects)
                obj.Free();

            base.OnUnload(e);
        }
    }
    public class SceneOptions
    {
    }

}