using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUImageProcessingSDX
{
    class GPUImageGame : Game
    {
        GraphicsDeviceManager graphicsDeviceManager;
        //the effect to change the texture
        Effect RenderImage;

        ImageFilter InitialFilter;
        ImageFilter BrightSquare;
        ImageFilter f,g;
        ImageFilter TerminalFilter;

        /// <summary>
        /// basic constructor. Just setting things up - pretty standard
        /// </summary>
        public GPUImageGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// This is called once the GraphicsDevice is all loaded up. 
        /// Load any effects, textures, etc
        /// </summary>
        protected override void LoadContent()
        {

            Texture2D InTex = Content.Load<Texture2D>("bigWalt.dds");

            RenderImage = Content.Load<Effect>("RenderToScreen.fxo");

            Effect blur = Content.Load<Effect>("Blur.fxo");
            Effect blur2 = Content.Load<Effect>("BLUR2.fxo");

            InitialFilter = new ImageFilter(RenderImage, RenderTarget2D.New(GraphicsDevice, GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, PixelFormat.B8G8R8A8.UNorm));
            InitialFilter.AddInput(InTex);

            InitialFilter.NextFilter = BrightSquare = new ImageFilter(blur, RenderTarget2D.New(GraphicsDevice, GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, PixelFormat.B8G8R8A8.UNorm));
            BrightSquare.AddInput(InitialFilter.RenderTarget);

            BrightSquare.NextFilter = f = new ImageFilter(blur2, RenderTarget2D.New(GraphicsDevice, GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, PixelFormat.B8G8R8A8.UNorm));
            f.AddInput(BrightSquare.RenderTarget);

            TerminalFilter = f;

            TerminalFilter.NextFilter = null;

            base.LoadContent();
        }

        /// <summary>
        /// Update doesn't do anything. Just here for completeness
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            ImageFilter curFilter = InitialFilter;

            while (curFilter != null)
            {
                curFilter.SendParametersToGPU();
                curFilter = curFilter.NextFilter;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// where we call the effect
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Draw(GameTime gameTime)
        {
            //reset the color of the screen...not really important since we are using effects
            GraphicsDevice.Clear(Color.CornflowerBlue);

            ImageFilter curFilter = InitialFilter;

            while (curFilter != null)
            {
                GraphicsDevice.SetRenderTargets(curFilter.RenderTarget);
                GraphicsDevice.DrawQuad(curFilter.RenderEffect);

                curFilter = curFilter.NextFilter;
            }

            GraphicsDevice.SetRenderTargets(GraphicsDevice.BackBuffer);
            RenderImage.Parameters["InputTexture"].SetResource(TerminalFilter.RenderTarget);
            GraphicsDevice.DrawQuad(RenderImage);

            base.Draw(gameTime);
        }


    }
}
