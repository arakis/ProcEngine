﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Aximo.Engine.Components.Geometry;
using Aximo.Engine.Windows;
using Aximo.Render;
using OpenToolkit.Mathematics;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Common.Input;
using OpenToolkit.Windowing.Desktop;

namespace Aximo.Engine
{
    public delegate void AfterApplicationInitializedDelegate();

    public class Application : IDisposable
    {
        private static Serilog.ILogger Log = Aximo.Log.ForContext<Application>();

        internal void SetConfig(ApplicationConfig config)
        {
            Config = config;
        }

        public event AfterApplicationInitializedDelegate AfterApplicationInitialized;

        public static Application Current { get; private set; }

        public Vector2i ScreenPixelSize => Window.Size;
        public float ScreenPixelAspectRation => (float)ScreenPixelSize.X / (float)ScreenPixelSize.Y;

        public Vector2i WindowLocation
        {
            get => Window.Location;
            set => Window.Location = value;
        }

        private ApplicationConfig Config;

        public bool IsMultiThreaded => Config.IsMultiThreaded;

        private float[] MouseSpeed = new float[3];
        private Vector2 MouseDelta;
        private float UpDownDelta;

        public Camera Camera => RenderContext.Camera;

        protected KeyboardState KeyboardState => Window.KeyboardState;

        public WindowContext WindowContext => WindowContext.Current;
        private GameWindow Window => WindowContext.Current.Window;

        private AutoResetEvent RunSyncWaiter;
        internal void RunSync()
        {
            RunSyncWaiter = new AutoResetEvent(false);
            Run();
            RunSyncWaiter.WaitOne();
            RunSyncWaiter?.Dispose();
            RunSyncWaiter = null;
        }

        internal virtual void Run()
        {
            Current = this;
            Init();
            AfterApplicationInitialized?.Invoke();
            WindowContext.Enabled = true;
        }

        public void Start()
        {
            Start(new ApplicationConfig());
        }
        public void Start(ApplicationConfig config)
        {
            Startup.Start(config, this);
        }

        public RenderContext RenderContext { get; private set; }
        public SceneContext SceneContext { get; private set; }
        internal Renderer Renderer { get; private set; }

        internal virtual void Init()
        {
            Process.GetCurrentProcess().PriorityBoostEnabled = true;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

            WindowContext.Init(Config);
            RegisterWindowEvents();

            Renderer = new Renderer
            {
                FlushRenderBackend = Config.FlushRenderBackend,
                UseShadows = Config.UseShadows,
                UseFrameDebug = Config.UseFrameDebug,
            };
            Renderer.Current = Renderer;

            RenderContext = new RenderContext()
            {
                // It's important to take a the size of the new created window instead of the startupConfig,
                // Because they may not be accepted or changed because of other DPI than 100%
                ScreenPixelSize = Window.Size,
            };
            RenderContext.Current = RenderContext;

            SceneContext = new SceneContext
            {
            };
            SceneContext.Current = SceneContext;

            RenderContext.Camera = new PerspectiveFieldOfViewCamera(new Vector3(2f, -5f, 2f), RenderContext.ScreenPixelSize.X / (float)RenderContext.ScreenPixelSize.Y)
            {
                NearPlane = 0.1f,
                FarPlane = 100.0f,
                Facing = 1.88f,
            };

            SceneContext.Init();

            OnLoadInternal();

            if (Config.InitializeAudio)
                Audio.AudioManager.Initialize();

            //ObjectManager.PushDebugGroup("Setup", "Scene");
            SetupScene();
            //ObjectManager.PopDebugGroup();

            //CursorVisible = false;

            StartFileListener();

            MovingObject = Camera;
            RenderContext.Camera.CameraChangedInternal += () =>
            {
                UpdateMouseWorldPosition();
            };

            RegisterWindowEventsStage2();
            Initialized = true;
        }

        private void RegisterWindowEvents()
        {
            // Dont's forget UnregisterWindowEvents!
            WindowContext.RenderFrame += (e) => OnRenderFrameInternal(e);
            WindowContext.UpdateFrame += (e) => OnUpdateFrameInternal(e);
            Window.Closing += (e) => OnClosingInternal(e);
            Window.Closed += OnClosed;
        }

        private void RegisterWindowEventsStage2()
        {
            Window.MouseMove += (e) => OnMouseMoveInternal(e);
            Window.KeyDown += (e) => OnKeyDownInternal(e);
            Window.MouseDown += (e) => OnMouseDownInternal(e);
            Window.MouseUp += (e) => OnMouseUpInternal(e);
            Window.MouseWheel += (e) => OnMouseWheelInternal(e);
            Window.Unload += () => OnUnloadInternal();
            Window.Resize += (e) => OnScreenResizeInternal(e);
            Window.FocusedChanged += (e) => OnFocusedChangedInternal(e);
        }

        private void UnregisterWindowEvents()
        {
            // TODO: Unregister is not correct, because of seperate delegate instance
            WindowContext.RenderFrame -= (e) => OnRenderFrameInternal(e);
            WindowContext.UpdateFrame -= (e) => OnUpdateFrameInternal(e);
            Window.MouseMove -= (e) => OnMouseMoveInternal(e);
            Window.KeyDown -= (e) => OnKeyDownInternal(e);
            Window.MouseDown -= (e) => OnMouseDownInternal(e);
            Window.MouseUp -= (e) => OnMouseUpInternal(e);
            Window.MouseWheel -= (e) => OnMouseWheelInternal(e);
            Window.Unload -= () => OnUnloadInternal();
            Window.Resize -= (e) => OnScreenResizeInternal(e);
            Window.FocusedChanged -= (e) => OnFocusedChangedInternal(e);
            Window.Closing -= (e) => OnClosingInternal(e);
            Window.Closed -= OnClosed;
        }

        protected virtual void SetupScene()
        {
        }

        private FileSystemWatcher ShaderWatcher;

        private void StartFileListener()
        {
            var shaderPath = Path.Combine(AssetManager.EngineRootDir, "Assets", "Shaders");
            if (!File.Exists(shaderPath))
                return;

            ShaderWatcher = new FileSystemWatcher(shaderPath);
            ShaderWatcher.Changed += (sender, e) =>
            {
                // Reload have to be in Main-Thread.
                DispatchUpdater(() => Reload());
            };
            ShaderWatcher.EnableRaisingEvents = true;
        }

        public bool IsFocused => WindowContext.IsFocused;

        /// <summary>
        /// Called from the Render thread before the render pipeline is invoked.
        /// If <see cref="IsMultiThreaded"/> is enabled, do not access the window and to not write to an <see cref="SceneObject"/>.
        /// </summary>
        protected virtual void OnRenderFrame(FrameEventArgs e) { }

        public int UpdateFrameNumber { get; private set; } = 0;
        public int RenderFrameNumber { get; private set; } = 0;

        private bool FirstRenderFrame = true;
        private bool FirstUpdateFrame = true;

        protected virtual void BeforeRenderFrame() { }
        protected virtual void AfterRenderFrame() { }

        protected bool RenderingEnabled { get; set; } = true;

        public EventCounter UpdateCounter = new EventCounter();
        public EventCounter RenderCounter = new EventCounter();

        private bool RenderInitialized = false;

        private void OnRenderFrameInternal(FrameEventArgs e)
        {
            if (!RenderInitialized)
            {
                Renderer.Init();
                RenderInitialized = true;
            }

            if (!RenderingEnabled || UpdateFrameNumber == 0)
                return;

            try
            {
                if (Closing)
                    return;

                if (FirstRenderFrame)
                    FirstRenderFrame = false;
                else
                    RenderFrameNumber++;

                RenderCounter.Tick();
                if (RenderCounter.Elapsed.TotalMilliseconds > 30 && RenderFrameNumber > 2 && IsFocused)
                    Log.Warn("SLOW Render: " + RenderCounter.Elapsed.ToString());

                RenderTasks.ProcessTasks();
                BeforeRenderFrame();

                if (Closing)
                    return;

                if (RenderFrameNumber <= 2)
                    Log.Verbose($"Render Frame #{RenderFrameNumber}");

                OnRenderFrame(e);

                if (Closing)
                    return;

                SceneContext.Sync();
                Renderer.Render();
                Window.SwapBuffers();
                AfterRenderFrame();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while rendering.");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private IPosition MovingObject;

        private bool DebugCamera;

        protected virtual void OnKeyDown(KeyboardKeyEventArgs e) { }

        private void OnKeyDownInternal(KeyboardKeyEventArgs e)
        {
            OnKeyDown(e);
            if (DefaultKeyBindings)
            {
                var kbState = Window.KeyboardState;
                if (kbState[Key.C])
                {
                    if (e.Shift)
                    {
                        DebugCamera = !DebugCamera;
                        var debugLine = SceneContext.GetActor("DebugLine").RootComponent as LineComponent;
                        debugLine.Visible = DebugCamera;
                        Console.WriteLine($"DebugCamera: {DebugCamera}");
                    }
                    else
                    {
                        MovingObject = Camera;
                    }
                }
                if (kbState[Key.F])
                {
                    Console.WriteLine("Dump infos:");
                    if (MovingObject != null)
                    {
                        Console.WriteLine($"MovingObject Position: {MovingObject.Position}");
                    }
                    Console.WriteLine($"Camer: Facing {Camera.Facing}, Pitch {Camera.Pitch}");
                }
                if (kbState[Key.B])
                {
                    MovingObject = RenderContext.GetObjectByName("Box1") as IPosition;
                }
                if (kbState[Key.L])
                {
                    MovingObject = RenderContext.GetObjectByName("StaticLight") as IPosition;
                }
                if (kbState[Key.J])
                {
                    Camera.Position = MovingObject.Position;
                }
            }
        }

        protected bool Initialized { get; private set; }

        public WindowBorder WindowBorder => Window.WindowBorder;

        protected void OnScreenResizeInternal(ResizeEventArgs e)
        {
            if (!Initialized)
                return;

            if (e.Size == RenderContext.ScreenPixelSize)
                return;

            var eventArgs = new ScreenResizeEventArgs(RenderContext.ScreenPixelSize, e.Size);

            Console.WriteLine("OnScreenResize: " + e.Size.X + "x" + e.Size.Y);
            RenderContext.ScreenPixelSize = e.Size;
            SceneContext.OnScreenResize(eventArgs);
            DispatchRender(() => RenderContext.OnScreenResize(eventArgs));
        }

        /// <summary>
        /// Called from the Update thread. Here you can update the world, for example positions, or execute game logic.
        /// If <see cref="IsMultiThreaded"/> is enabled, do not access any of the <see cref="IRenderObject"/> or any other internal renderer related objects.
        /// </summary>
        protected virtual void OnUpdateFrame(FrameEventArgs e) { }

        public bool DefaultKeyBindings = true;

        protected virtual void BeforeUpdateFrame() { }
        protected virtual void AfterUpdateFrame() { }

        private void OnUpdateFrameInternal(FrameEventArgs e)
        {
            if (Closing)
                return;

            if (FirstUpdateFrame)
                FirstUpdateFrame = false;
            else
                UpdateFrameNumber++;

            UpdateCounter.Tick();
            if (UpdateCounter.Elapsed.TotalMilliseconds > 30 && UpdateFrameNumber > 2 && IsFocused)
                Log.Warn("SLOW Update: " + UpdateCounter.Elapsed.ToString());

            SceneContext.UpdateTime();

            BeforeUpdateFrame();

            if (Closing)
                return;

            if (UpdateFrameNumber <= 2)
                Log.Verbose($"Update Frame #{UpdateFrameNumber}");

            foreach (var obj in SceneContext.UpdateFrameObjects.ToArray())
                obj.OnUpdateFrame();

            SceneContext.OnUpdateFrame();
            if (Closing)
                return;

            foreach (var obj in RenderContext.UpdateFrameObjects)
                obj.OnUpdateFrame();

            OnUpdateFrame(e);

            if (Closing)
                return;

            UpdaterTasks.ProcessTasks();

            if (!IsFocused)
            {
                return;
            }

            if (DefaultKeyBindings)
            {
                var input = Window.KeyboardState;

                if (input.IsKeyDown(Key.Escape))
                {
                    Stop();
                    return;
                }

                var kbState = Window.KeyboardState;

                IPosition pos = MovingObject;
                Camera cam = pos as Camera;
                bool simpleMove = cam == null;

                var stepSize = (float)(7.5f * e.Time);
                if (kbState[Key.ControlLeft])
                    stepSize *= 0.1f;

                if (kbState[Key.W])
                {
                    if (simpleMove)
                        pos.Position = new Vector3(
                            pos.Position.X,
                            pos.Position.Y + stepSize,
                            pos.Position.Z);
                    else
                        pos.Position = new Vector3(
                            pos.Position.X + ((float)Math.Cos(cam.Facing) * stepSize),
                            pos.Position.Y + ((float)Math.Sin(cam.Facing) * stepSize),
                            pos.Position.Z);
                }

                if (kbState[Key.S])
                {
                    if (simpleMove)
                        pos.Position = new Vector3(
                            pos.Position.X,
                            pos.Position.Y - stepSize,
                            pos.Position.Z);
                    else
                        pos.Position = new Vector3(
                            pos.Position.X - ((float)Math.Cos(cam.Facing) * stepSize),
                            pos.Position.Y - ((float)Math.Sin(cam.Facing) * stepSize),
                            pos.Position.Z);
                }

                if (kbState[Key.A])
                {
                    if (simpleMove)
                        pos.Position = new Vector3(
                            pos.Position.X - stepSize,
                            pos.Position.Y,
                            pos.Position.Z);
                    else
                        pos.Position = new Vector3(
                            pos.Position.X + ((float)Math.Cos(cam.Facing + (Math.PI / 2)) * stepSize),
                            pos.Position.Y + ((float)Math.Sin(cam.Facing + (Math.PI / 2)) * stepSize),
                            pos.Position.Z);
                }

                if (kbState[Key.D])
                {
                    if (simpleMove)
                        pos.Position = new Vector3(
                            pos.Position.X + stepSize,
                            pos.Position.Y,
                            pos.Position.Z);
                    else
                        pos.Position = new Vector3(
                            pos.Position.X - ((float)Math.Cos(cam.Facing + (Math.PI / 2)) * stepSize),
                            pos.Position.Y - ((float)Math.Sin(cam.Facing + (Math.PI / 2)) * stepSize),
                            pos.Position.Z);
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

                float reduce = 0.0035f * (float)e.Time;
                float reduceFactor = 1f - reduce;

                MouseSpeed[0] *= reduceFactor;
                MouseSpeed[1] *= reduceFactor;
                MouseSpeed[2] *= reduceFactor;

                MouseSpeed[0] = -(MouseDelta.X / 1.5f * (float)e.Time);
                MouseSpeed[1] = -(MouseDelta.Y / 1.5f) * (float)e.Time;
                MouseSpeed[2] = -(UpDownDelta / 1.5f) * (float)e.Time;
                MouseDelta = new Vector2();
                UpDownDelta = 0;

                if (cam != null)
                {
                    //Console.WriteLine(MouseSpeed[0]);
                    cam.Facing += MouseSpeed[0];
                    cam.Pitch += MouseSpeed[1];
                }
                else if (MovingObject is IScaleRotate rot)
                {
                    rot.Rotate = new Quaternion(
                        rot.Rotate.X + MouseSpeed[1],
                        rot.Rotate.Y,
                        rot.Rotate.Z + MouseSpeed[0]);
                }
                //Console.WriteLine(Camera.Pitch + " : " + Math.Round(MouseSpeed[1], 3));
                if (simpleMove)
                    pos.Position = new Vector3(
                        pos.Position.X,
                        pos.Position.Y,
                        pos.Position.Z + MouseSpeed[2]);
                else
                    pos.Position = new Vector3(
                        pos.Position.X,
                        pos.Position.Y,
                        pos.Position.Z + MouseSpeed[2]);

                if (kbState[Key.F11])
                {
                    Reload();
                }
            }

            // if (kbState[Key.F12])
            // {
            //     shadowFb.DestinationTexture.GetDepthTexture().Save("test.png");
            // }

            AfterUpdateFrame();
        }

        private TaskQueue UpdaterTasks = new TaskQueue();
        public void DispatchUpdater(Action task)
        {
            UpdaterTasks.Dispatch(task);
        }

        private TaskQueue RenderTasks = new TaskQueue();
        public void DispatchRender(Action task)
        {
            RenderTasks.Dispatch(task);
        }

        private void Reload()
        {
            foreach (var obj in RenderContext.AllObjects)
                if (obj is IReloadable reloadable)
                    reloadable.OnReload();
        }

        protected bool IsFirstUpdate => UpdateFrameNumber == 0;

        protected virtual void OnMouseMove(MouseMoveArgs e) { }

        private void OnMouseMoveInternal(MouseMoveEventArgs e)
        {
            var args = new MouseMoveArgs(e);

            // TODO: MIG
            // if (e.Mouse.LeftButton == ButtonState.Pressed)
            //     MouseDelta = new Vector2(e.XDelta, e.YDelta);

            var x = (float)(((double)e.X / (double)ScreenPixelSize.X * 2.0) - 1.0);
            var y = (float)(((double)e.Y / (double)ScreenPixelSize.Y * 2.0) - 1.0);

            CurrentMousePositionNDC = new Vector2(x, y);

            OnMouseMove(args);
            if (args.Handled)
                return;

            SceneContext.OnScreenMouseMove(args);

            // Console.WriteLine(CurrentMouseWorldPosition.ToString());
            // Console.WriteLine(CurrentMousePosition.ToString());
        }

        protected virtual void OnMouseDown(MouseButtonArgs e)
        {
        }

        private void OnMouseDownInternal(MouseButtonEventArgs e)
        {
            var args = new MouseButtonArgs(OldMouseButtonPos, CurrentMousePositionNDC, e);
            OldMouseButtonPos = CurrentMousePositionNDC;

            OnMouseDown(args);
            if (args.Handled)
                return;

            SceneContext.OnScreenMouseDown(args);
        }

        private Vector2 OldMouseButtonPos;
        protected virtual void OnMouseUp(MouseButtonArgs e)
        {
        }

        protected virtual void OnPostMouseUp(MouseButtonArgs e)
        {
        }

        private void OnMouseUpInternal(MouseButtonEventArgs e)
        {
            var args = new MouseButtonArgs(OldMouseButtonPos, CurrentMousePositionNDC, e);
            OldMouseButtonPos = CurrentMousePositionNDC;

            OnMouseUp(args);
            if (args.Handled)
                return;

            SceneContext.OnScreenMouseUp(args);

            OnPostMouseUp(args);
        }

        protected virtual void OnMouseWheel(MouseWheelEventArgs e) { }

        private void OnMouseWheelInternal(MouseWheelEventArgs e)
        {
            // TODO: MIG
            // OnMouseWheel(e);
            // if (DefaultKeyBindings)
            //     Camera.Fov -= e.DeltaPrecise;
        }

        protected void OnResize(EventArgs e)
        {
            // GL.Viewport(0, 0, RenderContext.ScreenSize.X, RenderContext.ScreenSize.Y);
            // Camera.AspectRatio = RenderContext.ScreenSize.X / (float)RenderContext.ScreenSize.Y;
        }

        private void OnFocusedChanged(FocusedChangedEventArgs e)
        {
        }

        private void OnFocusedChangedInternal(FocusedChangedEventArgs e)
        {
            OnFocusedChanged(e);
        }

        protected virtual void OnLoad() { }

        private void OnLoadInternal()
        {
            OnLoad();
        }

        protected virtual void OnUnload() { }

        private void OnUnloadInternal()
        {
            OnUnload();
        }

        protected virtual void OnClosing(CancelEventArgs e) { }

        private void OnClosingInternal(CancelEventArgs e)
        {
            OnClosing(e);
            if (e.Cancel)
                return;
            Stop();
        }

        protected virtual void OnClosed() { }

        private void OnClosedInternal()
        {
            OnClosed();
            Stop();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected bool Disposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                SignalShutdown();
                UnregisterWindowEvents();

                Thread.Sleep(200);

                AssetManager.ResetFileGenerator();

                RenderContext.Free();
                RenderContext = null;

                ShaderWatcher?.Dispose();
                ShaderWatcher = null;

                RunSyncWaiter?.WaitOne();
                RunSyncWaiter?.Dispose();
                RunSyncWaiter = null;

                Current = null;
            }

            Disposed = true;
        }

        private Vector2 _CurrentMousePosition;
        public Vector2 CurrentMousePositionNDC
        {
            get
            {
                return _CurrentMousePosition;
            }
            set
            {
                if (_CurrentMousePosition == value)
                    return;
                _CurrentMousePosition = value;
                UpdateMouseWorldPosition();
            }
        }

        public bool CurrentMouseWorldPositionIsValid { get; private set; }

        internal void UpdateMouseWorldPosition()
        {
            var pos = ScreenPositionToWorldPosition(CurrentMousePositionNDC);
            if (pos != null)
            {
                _CurrentMouseWorldPosition = (Vector3)pos;
                CurrentMouseWorldPositionIsValid = true;
                //Console.WriteLine(_CurrentMouseWorldPosition);
            }
            else
            {
                CurrentMouseWorldPositionIsValid = false;
            }
        }

        private Vector3 _CurrentMouseWorldPosition;
        public Vector3 CurrentMouseWorldPosition
        {
            get { return _CurrentMouseWorldPosition; }
        }

        protected Vector3? ScreenPositionToWorldPosition(Vector2 normalizedScreenCoordinates)
        {
            // FUTURE: Read Dept-Buffer to get the avoid ray-Cast and get adaptive Z-Position
            // Currently, it's fixed to 0 (Plane is at Z=0).
            var plane = new Plane(new Vector3(0, 0, 1), new Vector3(0, 0, 0));

            var pos1 = UnProject(normalizedScreenCoordinates, -1); // -1 requied for ortho. With z=0, perspective will not work, but no ortho
            var pos2 = UnProject(normalizedScreenCoordinates, 1);

            // if (!DebugCamera)
            // {
            //     var debugLine = ctx.GetObjectByName("DebugLine") as Line;
            //     // debugLine.SetPoint1(new Vector3(0, 0, 1));
            //     // debugLine.SetPoint2(new Vector3(3, 3, 1));
            //     debugLine.SetPoint1(pos1);
            //     debugLine.SetPoint2(pos2);
            //     debugLine.UpdateData();
            // }

            //Console.WriteLine($"{pos1} + {pos2}");

            var ray = Ray.FromPoints(pos1, pos2);
            if (plane.Raycast(ray, out float result))
                return (new Vector4(ray.GetPoint(result), 1) * RenderContext.WorldPositionMatrix).Xyz;

            return null;
        }

        public Vector3 UnProject(Vector2 mouse, float z)
        {
            Vector4 vec;

            vec.X = mouse.X;
            vec.Y = -mouse.Y;
            vec.Z = z;
            vec.W = 1.0f;

            vec = Vector4.Transform(vec, Camera.InvertedViewProjectionMatrix);

            return vec.Xyz / vec.W;
        }

        public double RenderFrequency => Window.RenderFrequency;
        public double UpdateFrequency => Window.UpdateFrequency;

        private bool _Closing;
        public bool Closing => _Closing || Window == null || Window.IsExiting;

        // Foreign Thread
        protected void SignalShutdown()
        {
            WindowContext.Enabled = false;
            _Closing = true;
        }

        public bool Stopped { get; private set; }

        // UI Thread
        public virtual void Stop()
        {
            if (_Closing || Stopped)
                return;

            SignalShutdown();
            Stopped = true;
            RunSyncWaiter?.Set();
        }

        /// <summary>
        /// Returns the pixel scale factor.
        /// </summary>
        public virtual Vector2 GetScreenPixelScale()
        {
            return Vector2.Divide(Config.NormalizedUISize, ScreenPixelSize.ToVector2());
        }

    }
}
