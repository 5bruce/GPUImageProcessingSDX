using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GPUImageProcessingSDX_Console
{


    public class GPUImageGame : Game
    {

        #region GLOBALS
        GraphicsDeviceManager graphicsDeviceManager;
        public static string ImageName = string.Empty;
        public static string OutputDirectory = string.Empty;
        /// <summary>
        /// basic effect to render an image to the screen
        /// </summary>
        Effect RenderImage;

        /// <summary>
        /// will change if an image is portrait vs landscape
        /// </summary>
        Effect ChangeOrientation;

        /// <summary>
        /// the final filter in the chain (you want this guys output)
        /// </summary>
        public ImageFilter TerminalFilter;

        /// <summary>
        /// A list that will hold all of the initial filters. These are filters which have textures/images as input instead of other filters
        /// </summary>
        public static List<InitialFilter> InitialFilters;

        /// <summary>
        /// true if the scene needs to be rendered (when a parameter changes)
        /// </summary>
        public static bool NeedsRender = true;

        /// <summary>
        /// When a new image is loaded, orientation is set to be the orientation of that image
        /// </summary>
        public DisplayOrientation Orientation = DisplayOrientation.Portrait;

        #endregion

        /// <summary>
        /// Basic constructor - initializes everything
        /// </summary>
        public GPUImageGame()
        {

            graphicsDeviceManager = new GraphicsDeviceManager(this);
            InitialFilters = new List<InitialFilter>();
            Content.RootDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\Content";

        }

        /// <summary>
        /// creates a new RenderTarget2D in the correct orientation
        /// </summary>
        /// <returns>the new RenderTarget2D</returns>
        private RenderTarget2D CreateRenderTarget2D(int w, int h)
        {
            //the only difference here is if the RT is created portrait or landscape
            if (Orientation == DisplayOrientation.Portrait)
                return ToDisposeContent(RenderTarget2D.New(GraphicsDevice, w, h, PixelFormat.B8G8R8A8.UNorm));
            else
                return ToDisposeContent(RenderTarget2D.New(GraphicsDevice, h, w, PixelFormat.B8G8R8A8.UNorm));
        }

       
        

        /// <summary>
        /// This is called once the GraphicsDevice is all loaded up. 
        /// Load any effects, textures, etc
        /// </summary>
        protected override void LoadContent()
        {

            foreach (InitialFilter i in InitialFilters)
            {

                Texture2D inputTex = Texture2D.New(GraphicsDevice, i.InputImage);

                i.AddInput(inputTex);

                //initial filters use a default input that just renders to the screen unless otherwise specified
                //i.RenderEffect = ToDisposeContent(File.Exists(Content.RootDirectory + "\\" + i.Path) ? Content.Load<Effect>(i.Path) : RenderImage);
   
                i.RenderEffect = new Effect(GraphicsDevice, File.ReadAllBytes(Content.RootDirectory + "\\" + i.Path));
                
                i.RenderTarget = CreateRenderTarget2D(inputTex.Width, inputTex.Height);

            }
            //make recurssive call for the rest
            foreach (ImageFilter i in InitialFilters)
            {
                LoadContentRec(i, true);
            }
            //print the effect tree after its all loaded up
            PrintTree();
            base.LoadContent();
        }

        /// <summary>
        /// Recursively add all the remaining children filters
        /// </summary>
        /// <param name="cur"></param>
        private void LoadContentRec(ImageFilter cur, bool isInitial = false)
        {

            //first load the effect
            if (!isInitial)
            {
                if (cur.Parents.Count <= 0)
                    return;

                cur.RenderEffect = new Effect(GraphicsDevice, File.ReadAllBytes(Content.RootDirectory + "\\" + cur.Path));
                int i = 0;
                if (cur.Parents[0].RenderTarget == null)
                    i = 1;
                cur.RenderTarget = CreateRenderTarget2D(cur.Parents[i].RenderTarget.Width, cur.Parents[i].RenderTarget.Height);
            }

            //add the parameters
            foreach (Parameter p in cur.Parameters)
            {
                if (p.Name == "ImageSize")
                {
                    p.Value = new Vector2(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height);
                }
            }
            //recurse
            foreach (ImageFilter i in cur.Children)
            {
                LoadContentRec(i);
            }

        }

        /// <summary>
        /// Update will update all the parameters of all the filters if they need to be updated. Recursively
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            //update all initial filters, then their children
            foreach (ImageFilter i in InitialFilters)
            {
                i.SendParametersToGPU();
                UpdateRec(i);
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// recursively update non-root filters
        /// </summary>
        /// <param name="cur"></param>
        private void UpdateRec(ImageFilter cur)
        {
            foreach (ImageFilter i in cur.Children)
            {
                i.SendParametersToGPU();
            }
        }

        /// <summary>
        /// Recursively call all the filters
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Draw(GameTime gameTime)
        {

            //reset the color of the screen...not really important since we are using effects
            GraphicsDevice.Clear(Color.CornflowerBlue);

            //A value has changed, or input has changed. We need to re-render
            if (NeedsRender)
            {
                //render each filter
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
                //the children will look at their parents to make sure they are all rendered - need to make sure they are set to rendered
                foreach (ImageFilter i in InitialFilters)
                {
                    ChangeNeedsRender(i);
                }

                NeedsRender = false;
            }

            TerminalFilter.RenderTarget.Save(OutputDirectory + "\\" + ImageName + ".jpg", ImageFileType.Jpg);

            this.Dispose();
        }

        /// <summary>
        /// recursively draw all non-initial nodes (ie. the ones with other filters as input)
        /// </summary>
        /// <param name="cur">The filter we are currently trying to render</param>
        private void DrawRec(ImageFilter cur)
        {
            //first check to make sure that all the parent filters have been drawn
            foreach (ImageFilter j in cur.Parents)
            {
                //if needRender is true. we need to render the parents
                if (j.NeedRender)
                {
                    return;
                }
            }
            //render current filter
            GraphicsDevice.SetRenderTargets(cur.RenderTarget);
            GraphicsDevice.DrawQuad(cur.RenderEffect);
            cur.NeedRender = false;

            foreach (ImageFilter i in cur.Children)
            {
                i.SendParametersToGPU();
                DrawRec(i);
            }
        }

        /// <summary>
        /// set the current filter and each child recursively to need render = true (ie. NEEDS to be rendered)
        /// </summary>
        /// <param name="cur"></param>
        private void ChangeNeedsRender(ImageFilter cur)
        {
            //
            cur.NeedRender = true;
            foreach (ImageFilter i in cur.Children)
            {
                ChangeNeedsRender(i);
            }
        }

        /// <summary>
        /// Recursively print the filter tree
        /// </summary>
        public void PrintTree()
        {
            //print out initial filters, then recursively call the children, indenting as we go
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

        /// <summary>
        /// The recursive print function
        /// </summary>
        /// <param name="cur">current node we are printing</param>
        /// <param name="level">how far in we are (ie. how many dashes to print before)</param>
        private void PrintTree(ImageFilter cur, int level = 1)
        {

            foreach (ImageFilter j in cur.Parents)
            {
                if (j.NeedRender)
                {
                    return;
                }
            }

            string s = "";
            //add a psace per level - 1
            for (int i = 0; i < level - 1; i++)
            {
                s += "   ";
            }
            //add one of these per line
            s += "|___";

            System.Diagnostics.Debug.WriteLine(s + cur.ToString());

            cur.NeedRender = false;

            //recurse
            foreach (ImageFilter i in cur.Children)
            {
                PrintTree(i, level + 1);
            }

        }


    }
}