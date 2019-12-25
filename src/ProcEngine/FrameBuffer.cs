﻿using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
//using System.Drawing.Imaging;

namespace ProcEngine
{

    public class FrameBuffer
    {
        private Texture _DestinationTexture;
        public Texture DestinationTexture => _DestinationTexture;

        private int _Handle;
        public int Handle => _Handle;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Bitmap GetTexture()
        {
            Bitmap bitmap = new Bitmap(Width, Height);
            var bits = bitmap.LockBits(new Rectangle(0, 0, Width, Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            //BindToRead(ReadBufferMode.ColorAttachment0 + AttachmentIndex);
            GL.ReadPixels(0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, bits.Scan0);
            //GL.BindTexture(TextureTarget.Texture2D, texColorBuffer);
            //GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgb, PixelType.UnsignedByte, bits.Scan0);
            bitmap.UnlockBits(bits);

            bitmap.Save("test.bmp");

            return bitmap;
        }

        public FrameBuffer(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void Init()
        {
            GL.GenFramebuffers(1, out _Handle);
            Use();

            _DestinationTexture = new Texture(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            _DestinationTexture.SetLinearFilter();
            _DestinationTexture.Use();

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _DestinationTexture.Handle, 0);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                throw new Exception();
        }

        public RenderBuffer RenderBuffer { get; private set; }

        public void CreateRenderBuffer()
        {
            RenderBuffer = new RenderBuffer(this);
        }

        public void Use()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _Handle);
        }

    }

}
