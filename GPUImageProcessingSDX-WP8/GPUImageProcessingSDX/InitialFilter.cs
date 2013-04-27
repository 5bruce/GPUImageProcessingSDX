using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace GPUImageProcessingSDX
{
    class InitialFilter : ImageFilter
    {

        /// <summary>
        /// the new image that we want to be input to this filter. ONLY FOR INITIAL FILTERS
        /// </summary>
        public WriteableBitmap NewImg;
        /// <summary>
        /// Goes with the NewImg. This is a list of all the inputs you would like to overwrite with NewImg. 
        /// You can leave this blank if there is only 1 input
        /// </summary>
        public List<int> OverwriteWith;

        public Dictionary<string, int> LoadFromContent;

        public string name = string.Empty;


        /// <summary>
        /// Constructor sets up the filter
        /// </summary>
        /// <param name="path">If you would like to load an image from Content, put its path here, else leave it blank</param>
        /// <param name="parameters">List of all the parameters you would like to add to the filter</param>
        public InitialFilter(string path = "", params Parameter[] parameters) :base(path, parameters)
        {
            OverwriteWith = new List<int>();
            LoadFromContent = new Dictionary<string,int>();
            GPUImageGame.InitialFilters.Add(this);

        }

        /// <summary>
        /// Add an input image which will be loaded from content
        /// </summary>
        /// <param name="path">The image path in content. Must be in DDS format</param>
        /// <param name="num">input texture position in HLSL shader</param>
        public void AddInput(string path, int num = -1)
        {
            if(path.Substring(path.Length - 3).ToUpper() != "DDS"){
                throw new Exception("Images must be in dds format. You can use Gimp to convert an image to DDS");
            }
            LoadFromContent.Add(path, num);
        }

    }
}
