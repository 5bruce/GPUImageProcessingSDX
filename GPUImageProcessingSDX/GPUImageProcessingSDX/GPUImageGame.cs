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
using System.Windows.Media.Imaging;

namespace GPUImageProcessingSDX
{
    class GPUImageGame : Game
    {

        public static void MemoryUse(string before = "", string after = "")
        {
            System.Diagnostics.Debug.WriteLine(before);
            System.Diagnostics.Debug.WriteLine("Lim:  {0}\nCur:  {1}\nPeak: {2}", DeviceStatus.ApplicationMemoryUsageLimit.ToString(), DeviceStatus.ApplicationCurrentMemoryUsage.ToString(), DeviceStatus.ApplicationPeakMemoryUsage.ToString());
            System.Diagnostics.Debug.WriteLine(after);
        }

        GraphicsDeviceManager graphicsDeviceManager;
        //the effect to change the texture
        Effect RenderImage;
        Effect ChangeOrientation;
        public ImageFilter TerminalFilter;

        public List<ImageFilter> Filters;

        public static List<ImageFilter> InitialFilters;
        public static bool NeedsRender = true;

        private RenderTarget2D OrientationRT;

        public DisplayOrientation Orientation = DisplayOrientation.Portrait;

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
            if(Orientation == DisplayOrientation.Portrait)
            return RenderTarget2D.New(GraphicsDevice, GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, PixelFormat.B8G8R8A8.UNorm);
            else
                return RenderTarget2D.New(GraphicsDevice, GraphicsDevice.BackBuffer.Height, GraphicsDevice.BackBuffer.Width, PixelFormat.B8G8R8A8.UNorm);
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

        public void LoadNewImage(ImageFilter filter, WriteableBitmap inputImage, int number = -1)
        {
            if (number == -1)
            {
                filter.Inputs.Clear();
            }
            else
            {
                for (int i = 0; i < filter.Inputs.Keys.Count; i++)
                {
                    if (filter.Inputs.ElementAt(i).Value == number)
                    {
                        filter.Inputs.Remove(filter.Inputs.ElementAt(i));
                        break;
                    }
                }
            }
            filter.InputImageStream = new KeyValuePair<WriteableBitmap, int>(inputImage, number);
            NeedsRender = true;
        }






        /// <summary>
        /// This is called once the GraphicsDevice is all loaded up. 
        /// Load any effects, textures, etc
        /// </summary>
        protected override void LoadContent()
        {

            FileStream fs = File.OpenRead("ChangeOrientation.fxo");
            try
            {

                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
                ChangeOrientation = new Effect(GraphicsDevice, bytes);

            }
            catch (Exception e)
            {

            }
            RenderImage = Content.Load<Effect>(@"HLSL\RenderToScreen.fxo");

            foreach (ImageFilter i in InitialFilters)
            {
                if (i.InputImageStream.Key == null || i.InputImageStream.Key == null)
                {
                    Texture2D tempTex = Content.Load<Texture2D>(i.Path);

                    if (CheckImage(tempTex.Width, tempTex.Height))
                    {
                        i.RenderEffect = RenderImage;
                        i.RenderTarget = CreateRT();
                        i.AddInput(tempTex);
                    }
                }
                else
                {
                    Texture2D tempTex = ToDisposeContent(Texture2D.New(GraphicsDevice, i.InputImageStream.Key.PixelWidth, i.InputImageStream.Key.PixelHeight, PixelFormat.B8G8R8A8.UNorm));
                    tempTex.SetData(i.InputImageStream.Key.Pixels);

                    if (CheckImage(tempTex.Width, tempTex.Height))
                    {

                        i.RenderEffect = RenderImage;
                        i.RenderTarget = CreateRT();

                        i.AddInput(tempTex, i.InputImageStream.Value);
                    }

                }

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
            ChangeOrientation.Parameters["InputTexture"].SetResource(TerminalFilter.RenderTarget);
            ChangeOrientation.Parameters["Orientation"].SetValue(Orientation == DisplayOrientation.Portrait ? 0 : Orientation == DisplayOrientation.LandscapeLeft ? 1 : 2);

            GraphicsDevice.DrawQuad(ChangeOrientation);

            //System.Diagnostics.Debug.WriteLine(Utility.CalculateFrameRate().ToString());

            MemoryUse("DRAW");

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

            if (tileIndex <= 0)
            {
                Picture p = saveToCameraRoll ? library.SavePictureToCameraRoll(imageName, imgStream) : library.SavePicture(imageName, imgStream);
                MessageBox.Show("Image Saved to 'Saved Pictures'");
            }

            string tempFile = imageName.Replace(".jpg", "_jpg.jpg");
            if (myStore.FileExists(tempFile))
                myStore.DeleteFile(tempFile);


            if (tileIndex > 0 && tileIndex < 10)
            {

                string fileName = "/Shared/ShellContent/tile" + tileIndex + ".jpg";

                using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(fileName, FileMode.OpenOrCreate, myStore))
                {
                    wbmp.SaveJpeg(isoStream, wbmp.PixelWidth, wbmp.PixelHeight, 0, 100);
                    MessageBox.Show("Image Added to Live Tiles.");

                }


            }

            myStore.Dispose();

        }

    }
}
