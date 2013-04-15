<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html>
    
    <head>
        <meta http-equiv="content-type" content="text/html; charset=iso-8859-1" />
        <meta name="Paul Demchuk" content="Toadums" />
        
        <title>GPU Image Processing Using SharpDX</title>
        <script src="//ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js"></script>
        <script src="//ajax.googleapis.com/ajax/libs/jqueryui/1.10.2/jquery-ui.min.js"></script>
        <script type="text/javascript" src="bootstrap/js/bootstrap.min.js"></script>
        <script type="text/javascript" src="dp.SyntaxHighlighter/Scripts/shCore.js"></script>
        <script type="text/javascript" src="dp.SyntaxHighlighter/Scripts/shBrushCSharp.js"></script>
        <script type="text/javascript" src="dp.SyntaxHighlighter/Scripts/shBrushCpp.js"></script>

        <link rel="stylesheet" type="text/css" href="bootstrap/css/bootstrap.css"/>
        <link rel="stylesheet" type="text/css" href="css/GPUImageProcessingSDX.css"/>
        <link rel="stylesheet" type="text/css" href="dp.SyntaxHighlighter/Styles/SyntaxHighlighter.css"/>



    </head>

    <body> 

    <div class="container">

        <div class="row" id="header"/>
            <div class="headerTitle">
                <div class="headerMain"> GPU Image Processing</div>
                <div class="headerSub"> Using SharpDX</div>
                <div class="headerAbstract">  
                    A framework to make Image Processing fast and easy on Windows Phone 8
                </div>
            </div>
        </div>

        <div class="row" id="content">
           <div class="accordion" id="accordion2">
            <div class="accordion-group">
              <div class="accordion-heading">
                <a class="accordion-toggle" data-toggle="collapse" data-parent="#accordion2" href="#collapseOne">
                  <h1>Motivation</h1>
                </a>
              </div>
              <div id="collapseOne" class="accordion-body collapse">
                <div class="accordion-inner">
                    <div class="subcontent">
                        <p>
                            I got started working on this project about half a year ago when my friend Matt told me that his boss needed a Windows Phone developer. 
                            I am a computer scientist, and am always looking for awesome opportunities to learn and improve my skills. 
                            So I went in to talk to his boss, my Honours supervisor, and my employer Bruce Gooch. 
                            I was basically hired on the spot, I believe mostly due to the fact that Bruce thinks that I look like Johnny Depp (a claim I will not deny). 
                        </p>
                        <p>
                            Bruce told me that he wanted a port of his image processing app "ToonPaint," which currently only exists on iPhone. 
                            ToonPaint takes an image on the phone, applies several filters to it, and makes it look cartoon-y. 
                            It looked like a cool enough app, so I agreed. Then Bruce told me that he wanted the app to run on the GPU. 
                            I had never even thought about what it would take to run code on the GPU, and didn't even know what the point of it was.
                        </p>
                        <p>
                            I spent a lot of time researching how to make this app happen. 
                            It turned out that the only way was to use DirectX and c++ to write native code for the Windows Phone 8 platform. 
                            I am comfortable working in c++, but DirectX was a whole new beast to me. 
                            I spent around fifty hours just playing with DirectX, c++, and reading about Microsoft's High Level Shader Language (HLSL). 
                        </p>
                        <p>
                            The hard part of the project was actually the code that was run on the CPU, not the GPU. 
                            The hard part was just executing a shader. I searched the internet high and low for a solution. 
                            I was keen on using C#, as that is my programming language of choice, but it just didn't look like that was an option. 
                            That is until I was pointed to the SharpDX library - which is basically just a c# wrapper for (almost) the entire DirectX library! 
                            I went back and forth between SharpDX and Native DirectX for a while. Neither seemed to be what I wanted. 
                            I didn't have the time to learn DirectX while I was in school, and SharpDX just didn't do what I needed it to do. 
                        </p>
                        <p>
                            And then the SharpDX 2.4.1 update came out. All of a sudden SharpDX was, and remains to be, an almost perfect solution. 
                            It let me use CUSTOM HLSL shaders from C# - it really was pure magic the first time that I was able to "Paint the Screen Red."
                            But there were still a lot of hardships - primarily with using the output of one shader as the input to another shader.
                            This is the driving reason that I wanted to make this project. 
                            It really is quite easy to do what I have done - if you have the right tools and know-how. 
                            The SharpDX library is amazing, and Alexandre Mutel is a genius for creating it; but I really like learning from examples. 
                            While creating this app, I have grown very intimate with a small part of the SharpDX library - the Toolkit. 
                            SharpDX.Toolkit is basically XNA, but more complete (the biggest feature for me was the ability to use Custom Effects on Windows Phones).
                        </p>
                        <p>
                            Anyways, enough of the backstory, lets get learning.
                        </p>
                        <p>
                            <strong>TL;DR:</strong> Image processing on Windows Phone 8, on the GPU, is way harder than it has to be, 
                            here is a library that gives you the infrastructure to do it easily. 
                        </p>
                    </div>
                </div>
              </div>
            </div>
            <div class="accordion-group">
              <div class="accordion-heading">
                <a class="accordion-toggle" data-toggle="collapse" data-parent="#accordion2" href="#collapseTwo">
                  <h1>Getting Started</h1>
                </a>
              </div>
              <div id="collapseTwo" class="accordion-body collapse">
                <div class="accordion-inner">

                    <p>
                        Although this library makes it easier to create an image processing app, there is still things that you need to learn - 
                        if you don’t already know them. Most importantly, you still need to write your own HLSL shader code, and some basic C#. 
                        The documentation that I found for HLSL was a little cryptic and dry, so I will try my best to break it down based on how I understand it. 
                        I am not guaranteeing you that this is correct, but it works for me, and hopefully it helps you.
                    </p>

                    <h2>SharpDX</h2>

                    <div class="subcontent">
                        <p>
                            You can download the SharpDX library <a href="http://sharpdx.org/">here</a>. I like to get the 
                            <a href="http://sharpdx-nightly-builds.googlecode.com/git/SharpDX-SDK-LatestDev.exe">latest nightly build</a>. 
                            It is important to make sure you do not download v2.4.2, as this version had bugs in SharpDX.Toolkit and WILL NOT WORK. 
                            Unpackage the library. The dlls that you will need to add references to are in:
                        </p>
                        <p>
                            <pre name="code" class="c">SharpDX/Bin/Standard-wp8-&lt;ARM for device, x86 for emulator&gt;</pre>
                        </p>
                        <p>
                            If you switch between the emulator and a device, you will need to re-add the corresponding dll’s, as well as change the configuration. 
                            (ARM for device, x86 for emulator)
                        </p>

                    </div>

                    <h2>HLSL</h2>

                    <div class="subcontent">
                        <p>
                            Like I said, you will still need to write HLSL code. If you are unfamiliar with HLSL, I have a few tips here - 
                            things that confused me at first. They won't all make sense right away, but overtime you will understand what they mean.
                        </p>
                        <p>
                            <ul>
                                <li><a href="http://msdn.microsoft.com/en-ca/library/windows/desktop/bb509638(v=vs.85).aspx">HLSL</a> syntax is very similar to c.</li>
                                <li>The <a href="http://msdn.microsoft.com/en-ca/library/windows/desktop/bb509626(v=vs.85).aspx">profile</a> that we use on WP8 is 4_0_level_9_3 shader. </li>
                                <li>Shaders must be PRE-COMPILED to be used on WP8. I will talk about how to do this later on</li>
                                <li>Texture Coordinates operate in UV Coordinates, from 0 to 1, not 0 to width/height. </li>
                                <li>For the purposes of this app, you do not need to worry about vertex shaders. You only need to use pixel shaders!</li>
                                <li>Finally and most importantly. When you compile a shader, if a variable is not used, it is NOT INCLUDED in the compiled assembly code. 
                                    So if in your c# code you try to set the parameter, it will throw an exception! 
                                    I have an example of this in the "Compiling Effects" section below.</li>
                            </ul>
                        </p>
                       
                    </div>

                    <h2>Pixel Shaders</h2>
                
                    <div class="subcontent">

                        <p>
                            A pixel shader is just a component that iterates over all the pixels (of the phone's screen in this case) and applies some operations to each.
                        </p>
                        <p>
                            The most basic pixel shader, "Paint it Red, just has a main function as follows:
                        </p>

                        <pre name="code" class="c">
                        float4 PSMain(float2 pos: TEXCOORD/*UV*/, float4 svn: SV_POSITION) : SV_TARGET
                        {
                            return float4(1,0,0,1);//foreach pixel, return red
                        }
                        </pre>

                    </div>
                </div>
              </div>
            </div>

            <div class="accordion-group">
              <div class="accordion-heading">
                <a class="accordion-toggle" data-toggle="collapse" data-parent="#accordion2" href="#collapseThree">
                  <h1>Code Samples</h1>
                </a>
              </div>
              <div id="collapseThree" class="accordion-body collapse">
                <div class="accordion-inner">

                    <h2>Writing Shaders</h2>
                    <div class="subcontent">
                        <p>
                            I find it easier to just copy an existing shader file when I make a new one. 
                            You can also use a text editor that saves files in plain text. 
                            If you use Notepad or Visual Studio to create a new file, they will add some header information, 
                            and you won't be able to compile it. Once you have the file, you can edit it in Visual Studio.
                        </p>
                        <p>
                            For your first effect, just have a look at the file *FirstFilter.fx that I included. 
                            You can modify this file as you want to make more complex effects.
                        </p>
                    </div>
                    
                    <h2>Compiling Shaders</h2>
                    <div class="subcontent">
                        <p>
                            To compile an effect takes a few steps:
                        </p>
                        <p>
                            <ul>
                                <li>Add the directory SharpDX/Bin/Win8Desktop-net40 to your path (just once)</li>
                                <li>Open a command prompt in the directory containing your uncompiled .fx file.</li>
                                <li>Enter the compile command:</li>
                            </ul>

                            <pre name="code" class="c">tkfxc FirstFilter.fx /FoFirstFilter.fxo //note there is no space after the /Fo</pre>

                        </p>
                        <p>
                            You can look at the output generated, if there are errors you will see red text, otherwise you are good to go. 
                            Like I said above, if you do not use a parameter in the code's control flow, it will not be added to the compiled code. 
                            For example the shader file:
                        </p>
                        
                        <pre name="code" class="c">
                        //GLOBALS
                        float3 position;
                        Texture2D input;
                        SamplerState Sampler;
                        float offset;

                        float4 PSMain(float2 pos: TEXCOORD/*UV*/, float4 svn: SV_POSITION) : SV_TARGET
                        {
                            offset = 3;
                            position = pos;
                            float3 color = input.Sample(Sampler, position);
                            return float4(1,0,0,1);
                        }
                        </pre>
                        
                        <p>
                            Will give you the exact same compiled assembly code as the Paint it Red" shader in the above section. 
                            Even though all of the global variables are used in the function, none of them have any influence on the return value (red). 
                            Yeah...the HLSL compiler is pretty smart!
                        </p>
                    </div>

                    <h2>Using Shaders</h2>

                    <div class="subcontent">
                        <p>
                            The first thing you need to do is compile the effect. 
                            Once you have done that, just add the .fxo to the project's Content folder. 
                            Then right click on the .fxo, and select properties. Change the Build Action to Content. 
                            (I am going to assume the effect is named FirstFilter.fxo for this example)
                        </p>
                        <p>
                            Now that you have your shader in the project, it is time to create your first Filter. 
                            In MainPage.xaml.cs, add a global variable for the InitialFilter and the Game:
                        </p>
                        
                        <pre name="code" class="c-sharp">
                        private InitialFilter FirstFilter;
                        private GPUImageGame Renderer;
                        </pre>

                        <p>
                            And in the constructor for MainPage, add the following:
                        </p>

                          
                        <pre name="code" class="c-sharp">
                        Renderer = new GPUImageGame();
                        FirstFilter = new InitialFilter(@"FirstFilter.fxo");

                        //this will be the filter which is rendered to the screen
                        Renderer.TerminalFilter = FirstFilter;
                        Renderer.Run(DisplayGrid);
                        </pre>
                                              
                        <p>
                            If you run the app, you should see the result of your first effect.
                        </p>
                  
                    </div>

                    <h2>Adding Image Inputs</h2>
                    
                    <div class="subcontent">
                        <p>
                            Assuming that you just used the provided FirstFilter.fxo, 
                            lets make a slightly more interesting effect - one that modifies a texture. 
                            Why don't we start off with something easy: inverting the picture in the y-axis.
                        </p>
                        <p>
                            All you need to do in c# is add an input to the FirstFilter. 
                            In the MainPage constructor, add the following line after you initialize the FirstFilter:
                        </p>
                        <p>
                            <pre name="code" class="c-sharp">FirstFilter.AddInput("cat.dds");</pre>
                        </p>
                        <p>
                            **notes** 
                        </p>
                        <p>
                            <ul>
                                <li>The input image has to be in DDS format on the Windows Phone 8 platform. 
                                    You can use Gimp to convert a file to DDS from any other format.</li>
                                <li>The image’s Build Action must be set to Content</li>
                            </ul>
                        </p>
                        <p>
                            Then change your FirstFilter.fx file to the following code, recompile, and add to your project.
                        </p>
                                                
                        <pre name="code" class="c">
                        //global variables
                        Texture2D InputTexture;//Textures MUST be named InputTexture[0-9]*
                        SamplerState Sampler;

                        float4 PSMain(float2 pos: TEXCOORD/*UV*/, float4 SVP : SV_POSITION) : SV_TARGET {
                            //Remember that UV coordinates are in [0,1]
                            return pixel = InputTexture.Sample(Sampler, float2(pos.x, 1-pos.y));
                        }

                        technique  {
                            pass {
                                Profile = 9.3;
                                PixelShader = PSMain;
                            }
                        }
                        </pre>
                        
                        <p>
                            With the code that is provided in MainPage.xaml.cs you can also load images from your library and invert them (fun). 
                            See the load_click function.
                        </p>
                    </div>
                    <h2>Chaining Filters together</h2>
                    <div class="subcontent">
                        <p>
                            Now for the important part - using filters as inputs to other filters. 
                            All you need to do is create another filter, and then pass your InitialFilter in to it. 
                            Here is an example of the c# code to do this:

                        <pre name="code" class="c-sharp">                       
                        //Globals
                        private InitialFilter FirstFilter;
                        private ImageFilter SecondFilter;
                        private GPUImageGame Renderer;

                        public MainPage(){

                            Rednerer = new GPUImageGame();
                            FirstFilter = new InitialFilter(@”FirstFilter.fxo”);

                            SecondFilter = new ImageFilter(@”SecondFilter.fxo”);
                            SecondFilter.AddInput(FirstFilter);//You want to render FirstFilter first

                            Renderer.TerminalFilter = SecondFilter;
                            Renderer.Run(DisplayGrid);

                        }
                        </pre>              
                    </div>
                </div>
              </div>
            </div>


          </div>
        </div>
      </div>
    </div>


    </body>
    <script language="javascript">
        window.onload = function () {
            dp.SyntaxHighlighter.ClipboardSwf = '/flash/clipboard.swf';
            dp.SyntaxHighlighter.HighlightAll('code');
        }
</script>
</html>
