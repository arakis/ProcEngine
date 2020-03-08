using OpenToolkit.Graphics.OpenGL4;
using System;

namespace AxEngine
{
    public class DeferredRenderPipeline : RenderPipeline
    {

        public FrameBuffer gBuffer;
        public Texture gPosition;
        public Texture gNormal;
        public Texture gAlbedoSpec;

        private Shader _DefLightShader;
        private float[] _vertices = DataHelper.Quad;

        private VertexArrayObject vao;
        private VertexBufferObject vbo;

        public override void Init()
        {
            var width = RenderContext.Current.ScreenSize.X;
            var height = RenderContext.Current.ScreenSize.Y;

            gBuffer = new FrameBuffer(width, height);
            gBuffer.InitNormal();

            gBuffer.ObjectLabel = nameof(gBuffer);

            gPosition = new Texture(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, width, height, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
            gPosition.ObjectLabel = nameof(gPosition);
            gPosition.SetNearestFilter();
            gBuffer.DestinationTextures.Add(gPosition);
            gBuffer.BindTexture(gPosition, FramebufferAttachment.ColorAttachment0);

            gNormal = new Texture(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, width, height, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
            gNormal.ObjectLabel = nameof(gNormal);
            gNormal.SetNearestFilter();
            gBuffer.DestinationTextures.Add(gNormal);
            gBuffer.BindTexture(gNormal, FramebufferAttachment.ColorAttachment1);


            gAlbedoSpec = new Texture(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            gAlbedoSpec.SetNearestFilter();
            gAlbedoSpec.ObjectLabel = nameof(gAlbedoSpec);
            gBuffer.DestinationTextures.Add(gAlbedoSpec);
            gBuffer.BindTexture(gAlbedoSpec, FramebufferAttachment.ColorAttachment2);

            GL.DrawBuffers(3, new DrawBuffersEnum[] {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2 });

            // var rboDepth = new RenderBuffer(gBuffer, RenderbufferStorage.DepthComponent, FramebufferAttachment.DepthAttachment);
            // rboDepth.ObjectLabel = nameof(rboDepth);

            // Attach default Forward Depth Buffer to this Framebuffer, so both share the same depth informations.
            var fwPipe = RenderContext.Current.GetPipeline<ForwardRenderPipeline>();
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fwPipe.FrameBuffer.RenderBuffer.Handle);

            gBuffer.Check();

            _DefLightShader = new Shader("Shaders/deferred-shading.vert", "Shaders/deferred-shading.frag");
            _DefLightShader.SetInt("gPosition", 0);
            _DefLightShader.SetInt("gNormal", 1);
            _DefLightShader.SetInt("gAlbedoSpec", 2);

            vbo = new VertexBufferObject();
            vbo.Create();
            vbo.Bind();

            var layout = new VertexLayout();
            layout.AddAttribute<float>(_DefLightShader.GetAttribLocation("aPos"), 2);
            layout.AddAttribute<float>(_DefLightShader.GetAttribLocation("aTexCoords"), 2);

            vao = new VertexArrayObject(layout, vbo);
            vao.Create();

            vao.SetData(_vertices);
        }

        public DeferredPass Pass;

        public override void Render(RenderContext context, Camera camera)
        {
            GL.Disable(EnableCap.Blend);

            ObjectManager.PushDebugGroup("OnRender Pass1", this);
            RenderPass1(context, camera);
            ObjectManager.PopDebugGroup();

            ObjectManager.PushDebugGroup("OnRender Pass2", this);
            RenderPass2(context, camera);
            ObjectManager.PopDebugGroup();

            GL.Enable(EnableCap.Blend);
        }

        private void RenderPass1(RenderContext context, Camera camera)
        {
            GL.Viewport(0, 0, context.ScreenSize.X, context.ScreenSize.Y);
            gBuffer.Bind();
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Pass = DeferredPass.Pass1;
            foreach (var obj in GetRenderObjects(context, camera))
                Render(context, camera, obj);
        }

        private void RenderPass2(RenderContext context, Camera camera)
        {
            Pass = DeferredPass.Pass2;
            // GL.ClearColor(0.1f, 0.3f, 0.3f, 1.0f);
            // GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Needed?
            // context.GetPipeline<ForwardRenderPipeline>().FrameBuffer.Use();
            // foreach (var obj in GetRenderObjects(context, camera))
            //     Render(context, camera, obj);

            ObjectManager.PushDebugGroup("OnRender LightShader", this);

            _DefLightShader.Bind();

            gPosition.Bind(TextureUnit.Texture0);
            gNormal.Bind(TextureUnit.Texture1);
            gAlbedoSpec.Bind(TextureUnit.Texture2);

            context.GetPipeline<DirectionalShadowRenderPipeline>().FrameBuffer.GetDestinationTexture().Bind(TextureUnit.Texture3);
            context.GetPipeline<PointShadowRenderPipeline>().FrameBuffer.GetDestinationTexture().Bind(TextureUnit.Texture4);

            _DefLightShader.SetVector3("viewPos", camera.Position);

            // TODO: Move to Pass1
            _DefLightShader.SetMaterial("material", Material.GetDefault());

            _DefLightShader.SetInt("shadowMap", 3);
            _DefLightShader.SetInt("depthMap", 4);
            _DefLightShader.BindBlock("lightsArray", context.LightBinding);
            _DefLightShader.SetInt("lightCount", context.LightObjects.Count);

            context.GetPipeline<ForwardRenderPipeline>().FrameBuffer.Bind();
            vao.Bind();
            GL.Disable(EnableCap.DepthTest);
            vao.Draw();
            GL.Enable(EnableCap.DepthTest);

            ObjectManager.PopDebugGroup();
        }

        public override void OnScreenResize()
        {
            gBuffer.Resize(RenderContext.Current.ScreenSize.X, RenderContext.Current.ScreenSize.Y);
        }
    }

    public enum DeferredPass
    {
        Pass1,
        Pass2,
    }

}