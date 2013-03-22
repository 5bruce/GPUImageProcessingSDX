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

        public ImageFilter TerminalFilter;

        public List<ImageFilter> Filters;

        public static List<ImageFilter> InitialFilters;
        public static bool NeedsRender = true;

        /// <summary>
        /// basic constructor. Just setting things up - pretty standard
        /// </summary>
        public GPUImageGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);
            InitialFilters = new List<ImageFilter>();
            Filters = new List<ImageFilter>();
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

            RenderImage = Content.Load<Effect>(@"HLSL\RenderToScreen.fxo");

            foreach (ImageFilter i in InitialFilters)
            {
                i.RenderEffect = RenderImage;
                i.RenderTarget = CreateRT();
                i.AddInput(Content.Load<Texture2D>(i.Path));
            }

            foreach (ImageFilter i in InitialFilters)
            {
                foreach (ImageFilter j in i.Children)
                {
                    LoadContentRec(j);
                }
            }

            PrintTree();
            base.LoadContent();
        }

        private void LoadContentRec(ImageFilter cur)
        {
            cur.RenderEffect = Content.Load<Effect>(cur.Path);
            cur.RenderTarget = CreateRT();

            foreach (Parameter p in cur.Parameters)
            {
                if (p.Name == "ImageSize")
                {
                    p.Value = new Vector2(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height);
                }
            }

            foreach (ImageFilter i in cur.Children)
            {
                LoadContentRec(i);
            }

        }

        /// <summary>
        /// Update doesn't do anything. Just here for completeness
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {

            foreach (ImageFilter i in InitialFilters)
            {
                i.SendParametersToGPU();
                UpdateRec(i);
            }

            base.Update(gameTime);
        }

        private void UpdateRec(ImageFilter cur)
        {
            foreach (ImageFilter i in cur.Children)
            {
                i.SendParametersToGPU();
            }
        }

        /// <summary>
        /// where we call the effect
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Draw(GameTime gameTime)
        {
            //reset the color of the screen...not really important since we are using effects
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (NeedsRender)
            {
                foreach (ImageFilter i in InitialFilters)
                {
                    GraphicsDevice.SetRenderTargets(i.RenderTarget);
                    GraphicsDevice.DrawQuad(i.RenderEffect);

                    i.NeedRender = false;

                    foreach (ImageFilter c in i.Children)
                    {
                        DrawRec(c);
                    }
                }

                //TODO need to add the "NeedsRender" bool...cant be rendering everything every time draw is called!!
                foreach (ImageFilter i in InitialFilters)
                {
                    ChangeNeedsRender(i);
                }

                NeedsRender = false;
            }

            GraphicsDevice.SetRenderTargets(GraphicsDevice.BackBuffer);
            RenderImage.Parameters["InputTexture"].SetResource(TerminalFilter.RenderTarget);
            GraphicsDevice.DrawQuad(RenderImage);

            System.Diagnostics.Debug.WriteLine(Utility.CalculateFrameRate().ToString());

            base.Draw(gameTime);
        }

        /// <summary>
        /// recursively draw all non-initial nodes (ie. the ones with other filters as input)
        /// </summary>
        /// <param name="cur"></param>
        /// <param name="level"></param>
        private void DrawRec(ImageFilter cur, int level = 1)
        {
            //first check to make sure that all the parent filters have been drawn
            foreach (ImageFilter j in cur.Parents)
            {
                if (j.NeedRender)
                {
                    return;
                }
            }

            GraphicsDevice.SetRenderTargets(cur.RenderTarget);
            GraphicsDevice.DrawQuad(cur.RenderEffect);
            cur.NeedRender = false;

            foreach (ImageFilter i in cur.Children)
            {
                i.SendParametersToGPU();
                DrawRec(i, level + 1);
            }
        }

        private void ChangeNeedsRender(ImageFilter cur)
        {
            cur.NeedRender = true;
            foreach (ImageFilter i in cur.Children)
            {
                ChangeNeedsRender(i);
            }
        }

        public void PrintTree()
        {

            foreach (ImageFilter i in InitialFilters)
            {

                i.NeedRender = false;
                System.Diagnostics.Debug.WriteLine(i.ToString());

                foreach (ImageFilter c in i.Children)
                {
                    PrintTree(c);
                }

            }
        }

        private void PrintTree(ImageFilter cur, int level = 1){

            foreach (ImageFilter j in cur.Parents)
            {
                if (j.NeedRender)
                {
                    return;
                }
            }

            string s = "";
            for (int i = 0; i < level-1; i++)
            {
                s += "   ";
            }

            s += "|___";

            System.Diagnostics.Debug.WriteLine(s + cur.ToString());

            cur.NeedRender = false;

            foreach (ImageFilter i in cur.Children)
            {
                PrintTree(i, level + 1);
            }

        }

    }
}
