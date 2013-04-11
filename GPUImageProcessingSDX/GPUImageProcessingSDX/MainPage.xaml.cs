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

namespace GPUImageProcessingSDX
{

    class Utility
    {
        #region Basic Frame Counter

        private static int lastTick;
        private static int lastFrameRate;
        private static int frameRate;

        public static int CalculateFrameRate()
        {
            if (System.Environment.TickCount - lastTick >= 1000)
            {
                lastFrameRate = frameRate;
                frameRate = 0;
                lastTick = System.Environment.TickCount;
            }
            frameRate++;
            return lastFrameRate;
        }
        #endregion

    }

    public partial class MainPage : PhoneApplicationPage
    {

        GPUImageGame Renderer;
        ImageFilter structureTensor, tensorSmoothing, flow, prepareForDOG, DOG, Threshold, LIC, toScreen;

        public static Dispatcher MyDispatcher;
        CameraCaptureTask ctask;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            AddEffects();
            MyDispatcher = this.Dispatcher;


            AddAppBar();

        }

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
            
            structureTensor = new ImageFilter(@"HLSL\ToonFXStructureTensorUsingSobelFilter.fxo", new Parameter("ImageSize", null));

            tensorSmoothing = new ImageFilter(@"HLSL\ToonFXGaussianFilter.fxo", new Parameter("texelWidthOffset", 0.0012f),
                new Parameter("texelHeightOffset", 0.0012f), new Parameter("sigma_flow", 2.66f));

            flow = new ImageFilter(@"HLSL\ToonFXFlowFromStructureTensor.fxo");

            prepareForDOG = new ImageFilter(@"HLSL\ToonFXPrepareForDogFilter.fxo");

            DOG = new ImageFilter(@"HLSL\ToonFXFlowDogFilter.fxo", new Parameter("texelWidthOffset", 0.0012f),
                new Parameter("texelHeightOffset", 0.0012f), new Parameter("sigma_dog", 0.9f + 0.9f * 1.0f));

            Threshold = new ImageFilter(@"HLSL\ToonFXThresholdDogFilter.fxo", new Parameter("edge_offset", 0.17f),
                new Parameter("grey_offset", 2.5f), new Parameter("black_offset", 2.65f));

            LIC = new ImageFilter(@"HLSL\ToonFXLineIntegralConvolutionFilter.fxo", new Parameter("texelWidthOffset", 0.0012f),
                new Parameter("texelHeightOffset", 0.0012f), new Parameter("sigma_c", 4.97f));
            
            GPUImageGame.InitialFilters.Add(toScreen = new ImageFilter());

           
            structureTensor.AddInput(toScreen);
            prepareForDOG.AddInput(toScreen);

            tensorSmoothing.AddInput(structureTensor);
            flow.AddInput(tensorSmoothing);

            DOG.AddInput(prepareForDOG, 1);
            DOG.AddInput(flow, 2);

            LIC.AddInput(DOG, 1);
            LIC.AddInput(flow, 2);

            Threshold.AddInput(LIC);
            
            Renderer.TerminalFilter = Threshold;

            Renderer.Run(DisplayGrid);

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
                Renderer.LoadNewImage(toScreen, PictureDecoder.DecodeJpeg(picture.GetImage()));

            }
            else if (queryStrings.ContainsKey("FileId"))
            {
                //launched from Edit
                MediaLibrary library = new MediaLibrary();
                Picture picture = library.GetPictureFromToken(queryStrings["FileId"]);
                Renderer.LoadNewImage(toScreen, PictureDecoder.DecodeJpeg(picture.GetImage()));

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
                    Renderer.LoadNewImage(toScreen, PictureDecoder.DecodeJpeg(ev.ChosenPhoto));

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
                    Renderer.LoadNewImage(toScreen, PictureDecoder.DecodeJpeg(ev.ChosenPhoto));

                }
            };

            ctask.Show();

        }

    }
}