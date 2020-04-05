﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Aximo.Engine;
using Aximo.Render;
using OpenToolkit;
using OpenToolkit.Mathematics;
using OpenToolkit.Windowing.Common;
using Xunit;

namespace Aximo.AxTests
{

    public class RenderApplicationTests : RenderApplication
    {

        // We need to ensure, that every unit test uses the same UI Thread,
        // otherwise GLFW will fail.
        protected static Thread UpdaterThread;

        protected static RenderApplicationTests CurrentTestApp;

        private static AutoResetEvent UpdaterThreadNextTaskWaiter;
        protected static void UpdaterThreadMain()
        {
            while (true)
            {
                UpdaterThreadNextTaskWaiter.WaitOne();
                try
                {
                    CurrentTestApp.Run();
                }
                catch (Exception ex)
                {
                    if (!CurrentTestApp.Exiting)
                        Console.WriteLine(ex);
                }
            }
        }

        private AutoResetEvent SetupWaiter;
        public RenderApplicationTests() : base(new RenderApplicationConfig
        {
            WindowTitle = "AxTests",
            WindowSize = new Vector2i(160, 120),
            WindowBorder = WindowBorder.Fixed,
            HideTitleBar = true,
        })
        {
            DebugHelper.LogThreadInfo("UnitTestThread");
            SetupWaiter = new AutoResetEvent(false);
            CurrentTestApp = this;
            AfterApplicationInitialized += () =>
            {
                SetupWaiter.Set();
            };
            if (UpdaterThread == null)
            {
                UpdaterThreadNextTaskWaiter = new AutoResetEvent(false);
                UpdaterThread = new Thread(UpdaterThreadMain);
                UpdaterThread.Start();
            }
            UpdaterThreadNextTaskWaiter.Set();
            SetupWaiter.WaitOne();
            //SetupWaiter.WaitOne(4000);
            SetupWaiter.Dispose();
            SetupWaiter = null;
            Console.WriteLine("Ready for tests");
        }

        private BufferComponent ScreenshotBuffer;

        protected override void SetupScene()
        {
            GameContext.BackgroundColor = new Vector4(0.2f, 0.3f, 0.3f, 1);
            GameContext.AddActor(new Actor(ScreenshotBuffer = new BufferComponent()));
        }

        protected override void BeforeUpdateFrame()
        {
            if (Exiting)
                return;

            if (IsMultiThreaded)
            {
                if (UpdateFrameNumber == 0)
                    UpdateWaiter.WaitOne();
                else
                    WaitHandle.SignalAndWait(RenderWaiter, UpdateWaiter);
            }
            else
            {
                if (WaitForRenderer)
                    return;

                if (UpdateFrameNumber == 0)
                {
                    UpdateWaiter.WaitOne();
                }
                else
                {
                    WaitHandle.SignalAndWait(TestWaiter, UpdateWaiter);
                }
            }
        }

        private bool WaitForRenderer = false;

        protected override void BeforeRenderFrame()
        {
            WaitForRenderer = false;

            if (Exiting)
                return;

            if (IsMultiThreaded)
            {
                if (RenderFrameNumber == 0)
                    RenderWaiter.WaitOne();
                else
                    WaitHandle.SignalAndWait(TestWaiter, RenderWaiter);
            }
        }

        public bool RendererEnabled;
        public AutoResetEvent UpdateWaiter = new AutoResetEvent(false);
        public AutoResetEvent RenderWaiter = new AutoResetEvent(false);
        public AutoResetEvent TestWaiter = new AutoResetEvent(false);

        public void RenderSingleFrameSync()
        {
            Console.WriteLine(" --- Render Single Frame ---");
            WaitForRenderer = true;
            WaitHandle.SignalAndWait(UpdateWaiter, TestWaiter);
        }

        public override void Dispose()
        {
            SignalShutdown();
            Console.WriteLine("Shutting down Test");
            ScreenshotBuffer?.Dispose();
            TestWaiter.Set();
            RenderWaiter.Set();
            UpdateWaiter.Set();

            SetupWaiter?.Dispose();
            SetupWaiter = null;

            base.Dispose();

            UpdateWaiter.Dispose();
            UpdateWaiter = null;
            RenderWaiter.Dispose();
            RenderWaiter = null;
            TestWaiter.Dispose();
            TestWaiter = null;
        }

        private string TestClassName => GetType().Name;
        private string TestOutputDir => Path.Combine(DirectoryHelper.GetAssetsPath("TestOutputs"), TestClassName);
        private string OriginalDir => TestOutputDir;
        private string DiffsDir => Path.Combine(DirectoryHelper.GetAssetsPath("TestOutputs"), "Diffs");
        public bool OverwriteOriginalImages;

        protected void RenderAndCompare(string testName)
        {
            RenderSingleFrameSync();
            var bmpCurrent = ScreenshotBuffer.BufferData.CreateBitmap();

            Directory.CreateDirectory(TestOutputDir);

            var originalFile = Path.Combine(OriginalDir, testName + ".png");
            var originalCopyFile = Path.Combine(DiffsDir, TestClassName + "." + testName + ".original.png");
            var currentFile = Path.Combine(DiffsDir, TestClassName + "." + testName + ".current.png");

            if (!File.Exists(originalFile) || OverwriteOriginalImages)
            {
                Directory.CreateDirectory(OriginalDir);
                bmpCurrent.Save(originalFile);

                if (File.Exists(currentFile))
                    File.Delete(currentFile);
                if (File.Exists(originalCopyFile))
                    File.Delete(originalCopyFile);
                return;
            }

            var bmpOriginal = Bitmap.FromFile(originalFile);

            var maxDiffAllowed = 1000;

            var diff = CompareImage(bmpCurrent, bmpOriginal, maxDiffAllowed);
            if (diff > maxDiffAllowed || diff == -1)
            {
                Directory.CreateDirectory(DiffsDir);
                bmpCurrent.Save(currentFile);
                Console.WriteLine($"MaxDifference: {diff} MaxDiffAllowed: {maxDiffAllowed}");
                File.Copy(originalFile, originalCopyFile, true);
            }
            else
            {
                if (File.Exists(currentFile))
                    File.Delete(currentFile);
                if (File.Exists(originalCopyFile))
                    File.Delete(originalCopyFile);
            }
            Assert.InRange(diff, 0, maxDiffAllowed);
        }

        public abstract class TestCasePipelineBase : TestCaseBase
        {
            public PipelineType Pipeline;
        }

        public abstract class TestCaseBase
        {

            public string ComparisonName;
            public TestCaseBase CompareWith;

            public override string ToString()
            {
                return ToString(true);
            }

            public string ToString(bool withComparison)
            {
                if (withComparison)
                    return ComparisonName + ToStringWithoutComparison();
                else
                    return ToStringWithoutComparison();
            }

            public abstract string ToStringWithoutComparison();

            protected abstract TestCaseBase CloneInternal();
            public TestCaseBase Clone()
            {
                var test = CloneInternal();
                test.ComparisonName = ComparisonName;
                if (CompareWith != null)
                    test.CompareWith = CompareWith.Clone();
                return test;
            }

            public T Clone<T>()
                where T : TestCaseBase
            {
                return (T)Clone();
            }

        }

        protected void Compare(string testCase, TestCaseBase test1, TestCaseBase test2)
        {
            Directory.CreateDirectory(TestOutputDir);

            var originalFile1 = Path.Combine(OriginalDir, testCase + test1.ToString() + ".png");
            var originalFile2 = Path.Combine(OriginalDir, testCase + test2.ToString() + ".png");
            var currentFile1 = Path.Combine(DiffsDir, TestClassName + "." + testCase + test1.ToStringWithoutComparison() + "." + test1.ComparisonName + ".current1.png");
            var currentFile2 = Path.Combine(DiffsDir, TestClassName + "." + testCase + test2.ToStringWithoutComparison() + "." + test2.ComparisonName + ".current2.png");

            Console.WriteLine($"Comparing Files: {originalFile1} {originalFile2}");

            if (!File.Exists(originalFile1))
                throw new Exception("Missing file: " + originalFile1);
            if (!File.Exists(originalFile2))
                throw new Exception("Missing file: " + originalFile2);

            var bmpOriginal1 = Bitmap.FromFile(originalFile1);
            var bmpOriginal2 = Bitmap.FromFile(originalFile2);

            var maxDiffAllowed = 1000;

            var diff = CompareImage(bmpOriginal1, bmpOriginal2, maxDiffAllowed);
            if (diff > maxDiffAllowed)
            {
                Console.WriteLine($"MaxDifference: {diff} MaxDiffAllowed: {maxDiffAllowed}");
                Directory.CreateDirectory(DiffsDir);
                File.Copy(originalFile1, currentFile1);
                File.Copy(originalFile2, currentFile2);
            }
            else
            {
                if (File.Exists(currentFile1))
                    File.Delete(currentFile1);
                if (File.Exists(currentFile2))
                    File.Delete(currentFile2);
            }
            Assert.InRange(diff, 0, maxDiffAllowed);
        }

        protected int CompareImage(Image img1, Image img2, int maxDiffAllowed)
        {

            if (img1 == null || img2 == null | img1.Width != img2.Width || img1.Height != img2.Height)
                return -1;

            var bmp1 = ResizeImage(img1);
            var bmp2 = ResizeImage(img2);
            var maxDiff = Analyzse(bmp1, bmp2, maxDiffAllowed);

            if (maxDiff > maxDiffAllowed)
            {
                Analyzse(bmp1, bmp2, maxDiffAllowed, true);
            }

            return maxDiff;
        }

        private int Analyzse(Bitmap bmp1, Bitmap bmp2, int maxDiffAllowed, bool showChanges = false)
        {
            int maxDiff = 0;

            if (showChanges)
                Console.WriteLine();

            for (var y = 0; y < bmp1.Height; y++)
            {
                for (var x = 0; x < bmp1.Width; x++)
                {
                    var dist = GetDistanceBetweenColours(bmp1.GetPixel(x, y), bmp2.GetPixel(x, y));

                    if (showChanges)
                        Console.Write(dist > maxDiffAllowed ? "#" : ".");

                    maxDiff = Math.Max(maxDiff, dist);
                }

                if (showChanges)
                    Console.WriteLine();
            }
            return maxDiff;
        }

        private static int GetDistanceBetweenColours(Color a, Color b)
        {
            int dR = a.R - b.R, dG = a.G - b.G, dB = a.B - b.B;
            return (dR * dR) + (dG * dG) + (dB * dB);
        }

        private Bitmap ResizeImage(Image image)
        {
            var pixelBlock = 3;
            int newWidth = (int)Math.Round(image.Width / (double)pixelBlock);
            int newHeight = (int)Math.Round((double)image.Height / (double)pixelBlock);
            Bitmap squeezed = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppRgb);
            Graphics canvas = Graphics.FromImage(squeezed);
            canvas.CompositingQuality = CompositingQuality.HighQuality;
            canvas.InterpolationMode = InterpolationMode.HighQualityBilinear;
            canvas.SmoothingMode = SmoothingMode.HighQuality;
            canvas.DrawImage(image, 0, 0, newWidth, newHeight);
            canvas.Flush();
            canvas.Dispose();
            return squeezed;
        }

        // public Actor GetDebugActor()
        // {
        //     Actor actor;
        //     GameContext.AddActor(actor = new Actor(new DebugCubeComponent()
        //     {
        //         Name = "Box2",
        //         Transform = GetTestTransform(),
        //         Material = GetTestMaterial(PipelineType.Deferred, new Vector3(0, 1, 0)),
        //     }));
        //     return actor;
        // }

        protected GameMaterial SolidColorMaterial(PipelineType pipelineType, Vector3 color)
        {
            return new GameMaterial()
            {
                Ambient = 0.2f,
                Color = color,
                PipelineType = pipelineType,
            };
        }

        protected GameMaterial SolidTextureMaterial(PipelineType pipelineType)
        {
            return new GameMaterial()
            {
                DiffuseTexture = GameTexture.GetFromFile("Textures/woodenbox.png"),
                SpecularTexture = GameTexture.GetFromFile("Textures/woodenbox_specular.png"),
                Ambient = 0.2f,
                PipelineType = pipelineType,
            };
        }

        protected Transform GetTestTransform()
        {
            return new Transform
            {
                Scale = new Vector3(1),
                Rotation = new Vector3(0, 0, 0.5f).ToQuaternion(),
                Translation = new Vector3(0f, 0, 0.5f),
            };
        }

        protected static PipelineType[] Pipelines = new PipelineType[] { PipelineType.Forward, PipelineType.Deferred };

        protected static object[] TestDataResult(TestCaseBase test)
        {
            return new object[] { test.Clone() };
        }

        protected static IEnumerable<object[]> GetComparePipelineTests(TestCasePipelineBase test)
        {
            if (!TestConfig.ComparePipelines)
                yield break;

            var test1 = test.Clone<TestCasePipelineBase>();
            test1.Pipeline = PipelineType.Forward;
            test1.ComparisonName = test1.Pipeline.ToString();

            var test2 = test.Clone<TestCasePipelineBase>();
            test2.Pipeline = PipelineType.Deferred;
            test2.ComparisonName = test2.Pipeline.ToString();

            test1.CompareWith = test2;

            yield return new object[] { test1 };
        }

    }

}