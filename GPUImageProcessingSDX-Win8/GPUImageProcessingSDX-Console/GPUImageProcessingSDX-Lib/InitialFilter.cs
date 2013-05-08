using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUImageProcessingSDX_Console
{
    public class InitialFilter : ImageFilter
    {

        public Image InputImage;

        /// <summary>
        /// Constructor sets up the filter
        /// </summary>
        /// <param name="path">If you would like to load an image from Content, put its path here, else leave it blank</param>
        /// <param name="parameters">List of all the parameters you would like to add to the filter</param>
        public InitialFilter(string path, Image img, params Parameter[] parameters) :base(path,parameters)
        {
            InputImage = img;
            GPUImageGame.InitialFilters.Add(this);

        }
    }
}
