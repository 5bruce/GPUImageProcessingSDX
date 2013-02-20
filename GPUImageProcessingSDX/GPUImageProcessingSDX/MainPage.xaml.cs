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
    public partial class MainPage : PhoneApplicationPage
    {

        GPUImageGame Renderer;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            Renderer = new GPUImageGame();
            Renderer.Run(DisplayGrid);

        }

    }
}