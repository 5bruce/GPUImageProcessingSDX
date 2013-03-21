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

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            AddEffects();
        }

        public void AddEffects()
        {
            Renderer = new GPUImageGame();

            ImageFilter structureTensor, tensorSmoothing, flow, prepareForDOG, DOG, Threshold, LIC, toScreen;

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

            GPUImageGame.InitialFilters.Add(toScreen = new ImageFilter("bigWalt.dds"));

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



    }
}