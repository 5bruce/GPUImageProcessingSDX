using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using GPUImageProcessingSDX.Resources;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Phone.Tasks;
using Microsoft.Phone;
using System.IO;
using SharpDX.Toolkit.Graphics;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework.Media;
using System.IO.IsolatedStorage;
using SharpDX;
using System.Threading.Tasks;

namespace GPUImageProcessingSDX
{

    public partial class MainPage : PhoneApplicationPage
    {
        #region GLOBALS

        /// <summary>
        /// You should be able to write a simple app without looking at this.
        /// </summary>
        GPUImageGame Renderer;
        /// <summary>
        /// my filters
        /// </summary>
        InitialFilter FirstFilter;
        private ImageFilter SecondFilter;

        /// <summary>
        /// We need to call the dispatcher from GPUImageGame to do some cross threading
        /// </summary>
        /// 
        public static Dispatcher MyDispatcher;
        /// <summary>
        /// Allow you to take pictures and use as input
        /// </summary>
        CameraCaptureTask ctask;

        #endregion

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            AddEffects();
            MyDispatcher = this.Dispatcher;


            AddAppBar();

        }
        /// <summary>
        /// initialize the (simple + boring) app bar
        /// </summary>
        private void AddAppBar()
        {
            ApplicationBar appBar = new ApplicationBar();

            ApplicationBarMenuItem load = new ApplicationBarMenuItem("Load");
            load.Click += new EventHandler(load_click);
            load.IsEnabled = true;
            appBar.MenuItems.Add(load);

            ApplicationBarMenuItem newImg = new ApplicationBarMenuItem("new");
            newImg.Click += new EventHandler(newImg_click);
            newImg.IsEnabled = true;
            appBar.MenuItems.Add(newImg);

            ApplicationBarMenuItem tile = new ApplicationBarMenuItem("Use as Live Tile");
            tile.Click += new EventHandler(tile_click);
            tile.IsEnabled = true;
            appBar.MenuItems.Add(tile);

            ApplicationBarMenuItem save = new ApplicationBarMenuItem("Save");
            save.Click += new EventHandler(save_click);
            save.IsEnabled = true;
            appBar.MenuItems.Add(save);

   

            ApplicationBar = appBar;

        }

        /// <summary>
        /// Add all the hlsl files here
        /// </summary>
        public void AddEffects()
        {

            Renderer = new GPUImageGame();
            FirstFilter = new InitialFilter(@"HLSL\RenderToScreen.fxo");
            FirstFilter.AddInput("cat.dds");

	        SecondFilter = new ImageFilter(@"HLSL\SpotLight.fxo", 
                new Parameter("ImageSize", new Vector2(1,1)),
                new Parameter("LightPos", new Vector2(400,400)));
	        SecondFilter.AddInput(FirstFilter);

	        Renderer.TerminalFilter = SecondFilter;

            Renderer.Run(DisplayGrid);
            Task t = Task.Factory.StartNew(new Action(() =>
            {
            Vector2 pos = new Vector2(0,0);
                int add = 1;
                while (true)
                {
                    pos.X = (pos.X + 1 * add);
                    if (pos.X == 800 || pos.X == 0)
                    {
                        pos.Y = (pos.Y + 40) % 1200;
                        add *= -1;
                    }

                    SecondFilter.UpdateParameter("LightPos", pos);

                    Thread.Sleep(1);
                }
            }));

        }

        /// <summary>
        /// Handles when the app is navigated to from:
        ///   o rightclick > apps... > *APPNAME*
        ///   o rickclick > Edit > *APPNAME*
        ///   o Launched from lenses
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            
            // Get a dictionary of query string keys and values.
            IDictionary<string, string> queryStrings = this.NavigationContext.QueryString;

            // Ensure that there is at least one key in the query string, and check whether the "token" key is present.
            if (queryStrings.ContainsKey("token"))
            {
                //launched from apps...
                MediaLibrary library = new MediaLibrary();
                Picture picture = library.GetPictureFromToken(queryStrings["token"]);
              //  Renderer.LoadNewImage(toScreen, PictureDecoder.DecodeJpeg(picture.GetImage()));

            }
            else if (queryStrings.ContainsKey("FileId"))
            {
                //launched from Edit
                MediaLibrary library = new MediaLibrary();
                Picture picture = library.GetPictureFromToken(queryStrings["FileId"]);
           //     Renderer.LoadNewImage(toScreen, PictureDecoder.DecodeJpeg(picture.GetImage()));

            }
            else
            {
                //launched from lens
                if (App.LaunchedFromLens)
                {
                    newImg_click(null, new EventArgs());
                    App.LaunchedFromLens = false;
                }
            }
        }
        
        /// <summary>
        /// User wants to save
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void save_click(object sender, EventArgs e)
        {
            Renderer.SaveImage("newImg.jpg", false);
        }

        /// <summary>
        /// User would like to load an existing photo from library
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void load_click(object sender, EventArgs e)
        {
            PhotoChooserTask photoChooserTask;

            photoChooserTask = new PhotoChooserTask();

            //picture loaded
            photoChooserTask.Completed += (s, ev) =>
            {
                if (ev.TaskResult == TaskResult.OK)
                {
                    //load to initial filter
                    if (GPUImageGame.InitialFilters.Count == 1)
                    {
                        Renderer.LoadNewImage(GPUImageGame.InitialFilters[0], PictureDecoder.DecodeJpeg(ev.ChosenPhoto));
                    }
                }
            };

            photoChooserTask.Show();

        }

        /// <summary>
        /// User wants to take a new picture with the camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void newImg_click(object sender, EventArgs e)
        {

            ctask = new CameraCaptureTask();

            //Photo captured
            ctask.Completed += (s, ev) =>
            {
                if (ev.TaskResult == TaskResult.OK)
                {
                    //load to initial filter
                    if (GPUImageGame.InitialFilters.Count == 1)
                    {
                        Renderer.LoadNewImage(GPUImageGame.InitialFilters[0], PictureDecoder.DecodeJpeg(ev.ChosenPhoto));
                    }

                }
            };

            ctask.Show();

        }

        /// <summary>
        /// User wants to use the current image as a live tile image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tile_click(object sender, EventArgs e)
        {
            CreateLiveTiles(true);
        }

        /// <summary>
        /// Set the live tiles. Live tiles must be saved in isostore:Shared\ShellContent\
        /// </summary>
        /// <param name="saveImage">True if you would like to save the current screen. It would be false if you were calling it from main for example</param>
        private void CreateLiveTiles(bool saveImage)
        {
            var myStore = IsolatedStorageFile.GetUserStoreForApplication();
            int tileCount = 0;

            //count how many tiles currently exist
            string[] s = myStore.GetFileNames("Shared\\ShellContent\\tile*.jpg");
            tileCount = s.Length;

            if (saveImage)
            {

                int tileNum = -1;
                //find out which number to save the new tile as (highest number available)
                if (tileCount < 9)
                {
                    tileNum = tileCount + 1;
                }
                else
                {//if there are already 9 tiles, remove the oldest one, and add the new one
                    for (int i = 2; i <= 9; i++)
                    {
                        myStore.CopyFile("Shared\\ShellContent\\tile" + i.ToString() + ".jpg", "Shared\\ShellContent\\tile" + (i - 1).ToString() + ".jpg");
                        tileNum = 9;
                    }
                }
                //save the tile to the storage. Call the normal saveImage method with an extra tile number parameter to indicate we want to save it as a tile, NOT to saved pics
                Renderer.SaveImage("TileImg.jpg", false, tileNum);
                tileCount = (tileCount + 1) % 9 + 1;
            }

            //if a tile was successfully saved, update the live tiles (or create them)
            if (tileCount > 0)
            {
                CycleTileData tileData = new CycleTileData()
                {
                    Title = "GPU-SDX",
                    Count = null,
                    SmallBackgroundImage =
                              new Uri("comicfx-icon159x159.png", UriKind.RelativeOrAbsolute),
                    CycleImages =

                    new Uri[]{  
                        new Uri("isostore:/Shared/ShellContent/tile1.jpg", UriKind.Absolute),
                        new Uri("isostore:/Shared/ShellContent/tile2.jpg", UriKind.Absolute),
                        new Uri("isostore:/Shared/ShellContent/tile3.jpg", UriKind.Absolute),
                        new Uri("isostore:/Shared/ShellContent/tile4.jpg", UriKind.Absolute),
                        new Uri("isostore:/Shared/ShellContent/tile5.jpg", UriKind.Absolute),
                        new Uri("isostore:/Shared/ShellContent/tile6.jpg", UriKind.Absolute),
                        new Uri("isostore:/Shared/ShellContent/tile7.jpg", UriKind.Absolute),
                        new Uri("isostore:/Shared/ShellContent/tile8.jpg", UriKind.Absolute),
                    }
                };

                //turn the tiles on!
                var mainTile = ShellTile.ActiveTiles.FirstOrDefault();
                if (null != mainTile)
                {
                    mainTile.Update(tileData);
                }

                MessageBox.Show("Image Added to Live Tiles.");


            }
        }

    }
}