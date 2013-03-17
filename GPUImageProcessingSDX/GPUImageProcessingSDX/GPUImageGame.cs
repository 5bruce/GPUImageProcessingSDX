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
        ImageFilter TerminalFilter;

        /// <summary>
        /// basic constructor. Just setting things up - pretty standard
        /// </summary>
        public GPUImageGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
        }

        private RenderTarget2D CreateRT()
        {
            return RenderTarget2D.New(GraphicsDevice, GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, PixelFormat.B8G8R8A8.UNorm);
        }

        /// <summary>
        /// This is called once the GraphicsDevice is all loaded up. 
        /// Load any effects, textures, etc
        /// </summary>
        protected override void LoadContent()
        {

            Texture2D InTex = Content.Load<Texture2D>("bigWalt.dds");
            RenderImage = Content.Load<Effect>(@"HLSL\RenderToScreen.fxo");

            ImageFilter structureTensor = new ImageFilter(Content.Load<Effect>(@"HLSL\ToonFXStructureTensorUsingSobelFilter.fxo"), CreateRT());

            ImageFilter tensorSmoothing = new ImageFilter(Content.Load<Effect>(@"HLSL\ToonFXGaussianFilter.fxo"), CreateRT(),new Parameter("texelWidthOffset",0.0012f),
                new Parameter("texelHeightOffset", 0.0012f), new Parameter("sigma_flow", 2.66f));

            ImageFilter flow = new ImageFilter(Content.Load<Effect>(@"HLSL\ToonFXFlowFromStructureTensor.fxo"), CreateRT());

            ImageFilter prepareForDOG = new ImageFilter(Content.Load<Effect>(@"HLSL\ToonFXPrepareForDogFilter.fxo"), CreateRT());

            ImageFilter DOG = new ImageFilter(Content.Load<Effect>(@"HLSL\ToonFXFlowDogFilter.fxo"), CreateRT(), new Parameter("texelWidthOffset", 0.0012f),
                new Parameter("texelHeightOffset", 0.0012f), new Parameter("sigma_dog", 0.9f + 0.9f * 1.0f));

            ImageFilter Threshold = new ImageFilter(Content.Load<Effect>(@"HLSL\ToonFXThresholdDogFilter.fxo"), CreateRT(), new Parameter("edge_offset", 0.17f),
                new Parameter("grey_offset", 2.5f), new Parameter("black_offset", 2.65f));

            ImageFilter LIC = new ImageFilter(Content.Load<Effect>(@"HLSL\ToonFXLineIntegralConvolutionFilter.fxo"), CreateRT(), new Parameter("texelWidthOffset", 0.0012f),
                new Parameter("texelHeightOffset", 0.0012f), new Parameter("sigma_c", 4.97f));

            ImageFilter toScreen = new ImageFilter(RenderImage, CreateRT());

            structureTensor.AddInput(toScreen);
            prepareForDOG.AddInput(toScreen);

            tensorSmoothing.AddInput(structureTensor);
            flow.AddInput(tensorSmoothing);

            DOG.AddInput(prepareForDOG,1);
            DOG.AddInput(flow,2);

            LIC.AddInput(DOG,1);
            LIC.AddInput(flow,2);

            Threshold.AddInput(LIC);



            InitialFilter = toScreen;
            InitialFilter.AddInput(InTex);

            toScreen.NextFilter = structureTensor;
            structureTensor.NextFilter = tensorSmoothing;
            tensorSmoothing.NextFilter = flow;
            flow.NextFilter = prepareForDOG;
            prepareForDOG.NextFilter = DOG;
            DOG.NextFilter = LIC;
            LIC.NextFilter = Threshold;

            TerminalFilter = Threshold;

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
