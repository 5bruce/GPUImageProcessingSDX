using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUImageProcessingSDX_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            GPUImageGame game = new GPUImageGame();

            
            if(args.Length < 1){
                Console.WriteLine("Need to specify an input image!");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Input file does not exist");
                return;

            }

            Image input = Image.Load(args[0]);
            int start = args[0].LastIndexOf('\\');
            int end = args[0].IndexOf('.');
            GPUImageGame.ImageName = args[0].Substring(start, end - start);
            GPUImageGame.OutputDirectory = args[1];

            ImageFilter ToonFXCMY64AutoContrastColorFilter,
                ToonFXCMY64ColorFilter,
                ToonFXFlowBilateralLineFilter1, ToonFXFlowBilateralLineFilter2,
                ToonFXFlowDogFilter,
                ToonFXFlowFromStructureTensor,
                ToonFXGaussianFilter1, ToonFXGaussianFilter2,
                ToonFXLineIntegralConvolutionFilter,
                ToonFXPrepareForDogFilter,
                ToonFXQuantizeColorUsingSobelFilter,
                ToonFXStructureTensorUsingSobelFilter,
                ToonFXThresholdDogFilter,
                GPUImageMultiplyBlendFilter;

            float texelOffset = 0.0012f,
             sigmaFlow = 2.66f,
             sigmaDog = 0.9f + 0.9f,
             licSigma = 4.97f,
             sigma_s = 5.4f,
             sigma_t = 8.0f,
             aniso = 2.0f,
             hueBins = 24.0f,
             satBins = 24.0f,
             valBins = 8.0f,
             edge_offset = 0.17f,
             grey_offset = 2.5f,
             black_offset = 2.65f;




            ToonFXCMY64AutoContrastColorFilter = new ImageFilter(@"HLSL\ToonFXCMY64AutoConstrastColorFilter.fxo");
            ToonFXCMY64ColorFilter = new ImageFilter(@"HLSL\ToonFXCMY64ColorFilter.fxo");
            ToonFXFlowBilateralLineFilter1 = new ImageFilter(@"HLSL\ToonFXFlowBilateralLineFilter.fxo", new Parameter("texelWidthOffset", texelOffset), new Parameter("texelHeightOffset", texelOffset), new Parameter("sigma_s", sigma_s), new Parameter("sigma_t", sigma_t), new Parameter("dir", true));
            ToonFXFlowBilateralLineFilter2 = new ImageFilter(@"HLSL\ToonFXFlowBilateralLineFilter.fxo", new Parameter("texelWidthOffset", texelOffset), new Parameter("texelHeightOffset", texelOffset), new Parameter("sigma_s", sigma_s), new Parameter("sigma_t", aniso * sigma_s), new Parameter("dir", false));
            ToonFXFlowDogFilter = new ImageFilter(@"HLSL\ToonFXFlowDogFilter.fxo", new Parameter("texelWidthOffset", texelOffset), new Parameter("texelHeightOffset", texelOffset), new Parameter("sigma_dog", sigmaDog));
            ToonFXFlowFromStructureTensor = new ImageFilter(@"HLSL\ToonFXFlowFromStructureTensor.fxo");
            ToonFXGaussianFilter1 = new ImageFilter(@"HLSL\ToonFXGaussianFilter.fxo", new Parameter("texelWidthOffset", texelOffset), new Parameter("texelHeightOffset", texelOffset), new Parameter("sigma_flow", sigmaFlow), new Parameter("dir", false));
            ToonFXGaussianFilter2 = new ImageFilter(@"HLSL\ToonFXGaussianFilter.fxo", new Parameter("texelWidthOffset", texelOffset), new Parameter("texelHeightOffset", texelOffset), new Parameter("sigma_flow", sigmaFlow), new Parameter("dir", true));
            ToonFXLineIntegralConvolutionFilter = new ImageFilter(@"HLSL\ToonFXLineIntegralConvolutionFilter.fxo", new Parameter("texelWidthOffset", texelOffset), new Parameter("texelHeightOffset", texelOffset), new Parameter("sigma_c", licSigma));
            ToonFXPrepareForDogFilter = new ImageFilter(@"HLSL\ToonFXPrepareForDogFilter.fxo");
            ToonFXQuantizeColorUsingSobelFilter = new ImageFilter(@"HLSL\ToonFXQuantizeColorsUsingFlowFilter.fxo", new Parameter("ValueBins", valBins), new Parameter("SaturationBins", satBins), new Parameter("HueBins", hueBins));
            ToonFXStructureTensorUsingSobelFilter = new ImageFilter(@"HLSL\ToonFXStructureTensorUsingSobelFilter.fxo", new Parameter("ImageSize", new Vector2(0, 0)));
            ToonFXThresholdDogFilter = new ImageFilter(@"HLSL\ToonFXThresholdDogFilter.fxo", new Parameter("edge_offset", edge_offset), new Parameter("grey_offset", grey_offset), new Parameter("black_offset", black_offset));
            GPUImageMultiplyBlendFilter = new ImageFilter(@"HLSL\GPUImageMultiplyBlendFilter.fxo");


            InitialFilter init = new InitialFilter(@"HLSL\RenderToScreen.fxo", input);

            ToonFXStructureTensorUsingSobelFilter.AddInput(init);
            ToonFXGaussianFilter1.AddInput(ToonFXStructureTensorUsingSobelFilter);
            ToonFXGaussianFilter2.AddInput(ToonFXStructureTensorUsingSobelFilter);
            ToonFXFlowFromStructureTensor.AddInput(ToonFXGaussianFilter2);
            ToonFXPrepareForDogFilter.AddInput(init);
            ToonFXFlowDogFilter.AddInput(ToonFXPrepareForDogFilter, 1);
            ToonFXFlowDogFilter.AddInput(ToonFXFlowFromStructureTensor, 2);
            ToonFXLineIntegralConvolutionFilter.AddInput(ToonFXFlowDogFilter, 1);
            ToonFXLineIntegralConvolutionFilter.AddInput(ToonFXFlowFromStructureTensor, 2);
            ToonFXThresholdDogFilter.AddInput(ToonFXLineIntegralConvolutionFilter);

            ToonFXFlowBilateralLineFilter1.AddInput(init, 1);
            ToonFXFlowBilateralLineFilter1.AddInput(ToonFXFlowFromStructureTensor,2);

            ToonFXFlowBilateralLineFilter2.AddInput(ToonFXFlowBilateralLineFilter1, 1);
            ToonFXFlowBilateralLineFilter2.AddInput(ToonFXFlowFromStructureTensor, 2);

            ToonFXQuantizeColorUsingSobelFilter.AddInput(ToonFXFlowBilateralLineFilter2, 1);
            ToonFXQuantizeColorUsingSobelFilter.AddInput(ToonFXFlowFromStructureTensor, 2);

            GPUImageMultiplyBlendFilter.AddInput(ToonFXQuantizeColorUsingSobelFilter, 1);
            GPUImageMultiplyBlendFilter.AddInput(ToonFXThresholdDogFilter, 2);


            game.TerminalFilter = ToonFXQuantizeColorUsingSobelFilter;

            game.Run();


        }
    }
}
