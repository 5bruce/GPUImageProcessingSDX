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

            ApplicationBarMenuItem save = new ApplicationBarMenuItem("Save");
            save.Click += new EventHandler(save_click);
            save.IsEnabled = true;
            appBar.MenuItems.Add(save);

            ApplicationBar = appBar;



        }

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

            GPUImageGame.InitialFilters.Add(toScreen = new ImageFilter("nature-hd-background.dds"));
            
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

      
        private void save_click(object sender, EventArgs e)
        {
            Renderer.SaveImage("newImg.jpg", false);
        }

        private void load_click(object sender, EventArgs e)
        {
            PhotoChooserTask photoChooserTask;

            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            photoChooserTask.Show();

        }

        /// <summary>
        /// A picture was chosen, load it up!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void photoChooserTask_Completed(object sender, PhotoResult e)
        {

            if (e.TaskResult == TaskResult.OK)
            {

                Renderer.LoadNewImage(toScreen, PictureDecoder.DecodeJpeg(e.ChosenPhoto));

                /*Render.NewImg = PictureDecoder.DecodeJpeg(e.ChosenPhoto);
                CallRender();
                EnableSaveButtons(true);
                ShowImage = false;
                HideAbout();
                */
            }

        }


    }
}