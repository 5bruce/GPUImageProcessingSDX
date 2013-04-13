using Microsoft.Phone;
using Microsoft.Phone.Info;
using Microsoft.Xna.Framework.Media;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GPUImageProcessingSDX
{
    class GPUImageGame : Game
    {
        /// <summary>
        /// Print out your memory usage
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        public static void MemoryUse(string before = "", string after = "")
        {
            System.Diagnostics.Debug.WriteLine(before);
            System.Diagnostics.Debug.WriteLine("Lim:  {0}\nCur:  {1}\nPeak: {2}", DeviceStatus.ApplicationMemoryUsageLimit.ToString(), DeviceStatus.ApplicationCurrentMemoryUsage.ToString(), DeviceStatus.ApplicationPeakMemoryUsage.ToString());
            System.Diagnostics.Debug.WriteLine(after);
        }

        #region GLOBALS
        GraphicsDeviceManager graphicsDeviceManager;
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
            Content.RootDirectory = "Content";

        }

        /// <summary>
        /// creates a new RenderTarget2D in the correct orientation
        /// </summary>
        /// <returns>the new RenderTarget2D</returns>
        private RenderTarget2D CreateRenderTarget2D()
        {
            //the only difference here is if the RT is created portrait or landscape
            if(Orientation == DisplayOrientation.Portrait)
                return ToDisposeContent(RenderTarget2D.New(GraphicsDevice, GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, PixelFormat.B8G8R8A8.UNorm));
            else
                return ToDisposeContent(RenderTarget2D.New(GraphicsDevice, GraphicsDevice.BackBuffer.Height, GraphicsDevice.BackBuffer.Width, PixelFormat.B8G8R8A8.UNorm));
        }

        /// <summary>
        /// Given the dimensions of the image, check if it is a landscape or portrait photo.
        /// Also, if it is a square image, inform the user that the ratio is bad, and it isn't going to look great.
        /// </summary>
        /// <param name="width">width of image</param>
        /// <param name="height">height of image</param>
        /// <returns>false if the user does NOT want to render a bad-ratio image. Else true</returns>
        private bool CheckImage(float width, float height)
        {
            bool success = true;
            float ratio = height / width;
            
            //check the ratio of the image to determine if it is portrait or landscape
            if (ratio < 2.2f && ratio > 1.2f)
            {
                Orientation = DisplayOrientation.Portrait;
            }
            else if (1.0f / ratio < 2.2f && 1.0f / ratio > 1.2f)
            {
                
                Orientation = DisplayOrientation.LandscapeLeft;
            }
            else if ((ratio < 1.2f && ratio >= 1.0f) || (1.0f / ratio < 1.2f && 1.0f / ratio >= 1.0f))
            {
                //the image was square(ish). Ask the user if they want to use this (it will squish and skew)
                MainPage.MyDispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBoxResult res = MessageBox.Show("This picture had a bad ratio. Are you sure you want to use it?", "Bad Ratio", MessageBoxButton.OKCancel);

                    if (res != MessageBoxResult.OK)
                    {
                        success = false;
                    }
                }));

                Orientation = ratio > 1 ? DisplayOrientation.Portrait : DisplayOrientation.LandscapeLeft;
            }

            return success;
        }

        /// <summary>
        /// When a new image is loaded (camera capture or photo chooser tasks), add it as input to the selected filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="inputImage"></param>
        /// <param name="list"></param>
        public void LoadNewImage(InitialFilter filter, WriteableBitmap inputImage, params int[] list)
        {
            //if there we no params, clear all inputs, and we want to add the input at spot -1 (= string.Empty)
            if (list.Length == 0)
            {
                filter.Inputs.Clear();
                filter.OverwriteWith.Add(-1);//add a -1 to the list
            }
            else
            {
                //otherwise for each current input, remove it if it's position is in the int[] list
                for (int i = 0; i < filter.Inputs.Keys.Count; i++)
                {
                    foreach (int n in list)
                    {
                        if (filter.Inputs.ElementAt(i).Value == n)
                        {
                            filter.Inputs.Remove(filter.Inputs.ElementAt(i));
                            filter.OverwriteWith.Add(n);
                        }
                    }
                }
            }

            //we do not want to load an image from the content
            filter.NewImg = inputImage;

            NeedsRender = true;
        }

        /// <summary>
        /// This is called once the GraphicsDevice is all loaded up. 
        /// Load any effects, textures, etc
        /// </summary>
        protected override void LoadContent()
        {
            //Read in the changeOrientation effect, and load it up
            FileStream fs = File.OpenRead("ChangeOrientation.fxo");
            
            byte[] bytes = new byte[fs.Length];
            fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
            fs.Close();
            ChangeOrientation = ToDisposeContent(new Effect(GraphicsDevice, bytes));
            
            RenderImage = ToDisposeContent(Content.Load<Effect>(@"RenderToScreen.fxo"));

            foreach (InitialFilter i in InitialFilters)
            {

                if (i.LoadFromContent.Count > 0)
                {
                    foreach(KeyValuePair<string, int> kvp in i.LoadFromContent){

                        Texture2D texture = ToDisposeContent(Content.Load<Texture2D>(kvp.Key));
                        i.AddInput(texture, kvp.Value);
                    }
                }


                if (i.OverwriteWith.Count > 0)
                {
                    //make sure there is atleast 1 input to load
                    try
                    {
                        //load the texture from the writeableBitmap passed in
                        Texture2D texture = ToDisposeContent(Texture2D.New(GraphicsDevice, i.NewImg.PixelWidth, i.NewImg.PixelHeight, PixelFormat.B8G8R8A8.UNorm));
                        texture.SetData(i.NewImg.Pixels);
                        foreach (int n in i.OverwriteWith)
                        {
                            i.AddInput(texture, n);
                        }
                    }
                    finally
                    {
                        //manage some memory!!!!!!
                        i.OverwriteWith.Clear();
                        i.NewImg = null;
                        GC.Collect();
                    }
                }
                
                //initial filters use a default input that just renders to the screen unless otherwise specified
                i.RenderEffect = ToDisposeContent(File.Exists(Content.RootDirectory + "\\" + i.Path) ? Content.Load<Effect>(i.Path) : RenderImage);
                i.RenderTarget = CreateRenderTarget2D();
                
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
                cur.RenderEffect = ToDisposeContent(Content.Load<Effect>(cur.Path));
                cur.RenderTarget = CreateRenderTarget2D();
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
            //draw the image to the screen. Rotate it if it is portrait
            GraphicsDevice.SetRenderTargets(GraphicsDevice.BackBuffer);
            ChangeOrientation.Parameters["InputTexture"].SetResource(TerminalFilter.RenderTarget);
            ChangeOrientation.Parameters["Orientation"].SetValue(Orientation == DisplayOrientation.Portrait ? 0 : Orientation == DisplayOrientation.LandscapeLeft ? 1 : 2);

            GraphicsDevice.DrawQuad(ChangeOrientation);

            base.Draw(gameTime);
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
        private void PrintTree(ImageFilter cur, int level = 1){

            foreach (ImageFilter j in cur.Parents)
            {
                if (j.NeedRender)
                {
                    return;
                }
            }

            string s = "";
            //add a psace per level - 1
            for (int i = 0; i < level-1; i++)
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

        /// <summary>
        /// The worst code I have ever had to write. As of right now, I don't see any better way.
        /// It is still very, very fast, even though it looks like it would be super slow.
        /// The only -real- problem is that the int[] buff seems to throw the memory usage over the top :(
        /// </summary>
        public void SaveImage(string saveFileName, bool saveToCameraRoll, int tileIndex = -1)
        {

            IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();
            WriteableBitmap wbmp = new WriteableBitmap(TerminalFilter.RenderTarget.Width, TerminalFilter.RenderTarget.Height);

            TerminalFilter.RenderTarget.GetData(wbmp.Pixels);


            //create a writeablebitmap. This is going to be what is actually saved
            WriteableBitmap newWbmp = null;
            //buff will hold all the pixels of tempimage
            int[] buff = new int[wbmp.Pixels.Length];
            //get the pixels of tempimage and save them in buff

            //save the image as a jpg. We need to make sure the orientation is right first though
            using (IsolatedStorageFileStream isoStream2 = new IsolatedStorageFileStream("new.jpg", FileMode.OpenOrCreate, isoStore))
            {
                wbmp.SaveJpeg(isoStream2, wbmp.PixelWidth, wbmp.PixelHeight, 0, 100);
            }

            //load the previously saved jpg
            using (IsolatedStorageFileStream isoStream2 = new IsolatedStorageFileStream("new.jpg", FileMode.OpenOrCreate, isoStore))
            {
                SaveImage(isoStream2, tileIndex, newWbmp ?? wbmp, saveFileName, saveToCameraRoll);
            }


            if (isoStore.FileExists("new.jpg"))
                isoStore.DeleteFile("new.jpg");

        }

        /// <summary>
        /// Method which actually saves the image to the cameraroll.
        /// This code was provided to me by Nokia
        /// </summary>
        /// <param name="imgStream">Stream of the image that we want to save</param>
        private void SaveImage(Stream imgStream, int tileIndex, WriteableBitmap wbmp, string saveFileName, bool saveToCameraRoll)
        {

            MediaLibrary library = new MediaLibrary();
            string imageName = saveFileName;
            var myStore = IsolatedStorageFile.GetUserStoreForApplication();
            //tileIndex will be < = if we want to save to cameraroll/saved pictures
            if (tileIndex <= 0)
            {
                Picture p = saveToCameraRoll ? library.SavePictureToCameraRoll(imageName, imgStream) : library.SavePicture(imageName, imgStream);
                MessageBox.Show("Image Saved to 'Saved Pictures'");
            }
            //remove the file from isolated storage
            string tempFile = imageName.Replace(".jpg", "_jpg.jpg");
            if (myStore.FileExists(tempFile))
                myStore.DeleteFile(tempFile);

            //otherewise save to live tiles
            if (tileIndex > 0 && tileIndex < 10)
            {

                string fileName = "/Shared/ShellContent/tile" + tileIndex + ".jpg";

                using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(fileName, FileMode.OpenOrCreate, myStore))
                {
                    wbmp.SaveJpeg(isoStream, wbmp.PixelWidth, wbmp.PixelHeight, 0, 100);
                }


            }

            myStore.Dispose();

        }

    }
}
