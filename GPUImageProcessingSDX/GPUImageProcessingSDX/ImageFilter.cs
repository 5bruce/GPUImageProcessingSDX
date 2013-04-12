using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace GPUImageProcessingSDX
{
    class ImageFilter
    {

        #region GLOBALS

        /// <summary>
        /// The Effect for this filter
        /// </summary>
        private Effect m_Effect;
        /// <summary>
        /// The render target for this filter
        /// </summary>
        private RenderTarget2D m_RenderTarget;
        /// <summary>
        /// list of all filters that rely on this filter to complete first
        /// </summary>
        public List<ImageFilter> Children;
        /// <summary>
        /// list of all filters that need to be applied before this one can be
        /// </summary>
        public List<ImageFilter> Parents;
        /// <summary>
        /// list of the parameters for the filter
        /// </summary>
        public List<Parameter> Parameters;
        /// <summary>
        /// dictionary of inputs (either rendertargets or texture2ds). This is what is actually passed to the GPU to be input to the filter
        /// </summary>
        public Dictionary<object, int> Inputs;
        /// <summary>
        /// if true, this filter has not been rendered yet
        /// </summary>
        public bool NeedRender = true;
        /// <summary>
        /// Set the path if you would like to load the file from content (ie. not from media library)
        /// </summary>
        public string Path = string.Empty;
        /// <summary>
        /// the new image that we want to be input to this filter. ONLY FOR INITIAL FILTERS
        /// </summary>
        public WriteableBitmap NewImg;
        /// <summary>
        /// Goes with the NewImg. This is a list of all the inputs you would like to overwrite with NewImg. 
        /// You can leave this blank if there is only 1 input
        /// </summary>
        public List<int> OverwriteWith;
        
        /// <summary>
        /// Gets or sets the effect associated with this filter
        /// </summary>
        public Effect RenderEffect
        {
            get { return m_Effect; }
            set { m_Effect = value; }
        }

        /// <summary>
        /// Gets or sets the render target associated with this filter
        /// </summary>
        public RenderTarget2D RenderTarget
        {
            get { return m_RenderTarget; }
            set { m_RenderTarget = value; }
        }

        #endregion

        /// <summary>
        /// Constructor sets up the filter
        /// </summary>
        /// <param name="path">If you would like to load an image from Content, put its path here, else leave it blank</param>
        /// <param name="parameters">List of all the parameters you would like to add to the filter</param>
        public ImageFilter(string path = "", params Parameter[] parameters)
        {

            Children = new List<ImageFilter>();
            Parents = new List<ImageFilter>();
            Inputs = new Dictionary<object, int>();

            OverwriteWith = new List<int>();

            if (path != "")
            {
                Path = path;
            }

            Parameters = new List<Parameter>();
            //copy paramters to the filter
            foreach (Parameter par in parameters)
            {
                Parameters.Add(par);
            }
        }
        
        /// <summary>
        /// Specifies that you would like the input of the filter to be another filter
        /// </summary>
        /// <param name="imfil">The filter to use as input</param>
        /// <param name="num">If there is one input to the fitler, leave blank and name the input "InputTexture" in the HLSL. If there is multiple inputs, name them "InputTexture[0-9]*", and put that number here</param>
        public void AddInput(ImageFilter imfil, int num = -1)
        {
            //make sure the parent/child relation holds
            Parents.Add(imfil);
            imfil.Children.Add(this);
            Inputs.Add(imfil, num);
        }

        /// <summary>
        /// Specifies that you would like the input of the filter to be a rendertarget
        /// </summary>
        /// <param name="imfil">The RT to use as input</param>
        /// <param name="num">If there is one input to the fitler, leave blank and name the input "InputTexture" in the HLSL. If there is multiple inputs, name them "InputTexture[0-9]*", and put that number here</param>
        public void AddInput(RenderTarget2D rt, int num = -1)
        {
            Inputs.Add(rt, num);
        }

        /// <summary>
        /// Specifies that you would like the input of the filter to be a texture
        /// </summary>
        /// <param name="imfil">The texture to use as input</param>
        /// <param name="num">If there is one input to the fitler, leave blank and name the input "InputTexture" in the HLSL. If there is multiple inputs, name them "InputTexture[0-9]*", and put that number here</param>
        public void AddInput(Texture2D tex, int num = -1)
        {
            Inputs.Add(tex, num);
        }

        /// <summary>
        /// update the specified parameter value
        /// </summary>
        /// <param name="name">The parameter to update</param>
        /// <param name="value">The value to change to</param>
        /// <returns>true if the parameter is found, and updated sucessfully.</returns>
        public bool UpdateParameter(string name, object value)
        {
            bool success = false;

            foreach (Parameter p in Parameters)
            {   
                //find the correct parameter and update it
                if (p.Name == name)
                {
                    p.Value = value;
                    success = true;
                    p.HasChanged = true;
                    GPUImageGame.NeedsRender = true;//parameter is changed! Need to re-render everything
                    break;
                }
            }
            return success;
        }

        /// <summary>
        /// Send all the parameters from the GPU to CPU - only if they have changed
        /// </summary>
        public virtual void SendParametersToGPU()
        {
            foreach (Parameter param in Parameters)
            {
                if (param.HasChanged)
                {
                    //Value type and reference types have different methods to send to the GPU, we need to distinguish a difference
                    if (RenderEffect.Parameters[param.Name].IsValueType)
                    {
                        EffectParameter p = RenderEffect.Parameters[param.Name];
                        EffectParameterType type = p.ParameterType;
 
                        try
                        {
                            //need to switch to find out the type of the parameter - we need to cast before we send to the GPU! Then send
                            switch (type)
                            {
                                case EffectParameterType.Float:
                                    if (p.ColumnCount == 2)
                                    {

                                        RenderEffect.Parameters[param.Name].SetValue((Vector2)param.Value);
                                    }
                                    else
                                    {
                                        RenderEffect.Parameters[param.Name].SetValue((float)param.Value);

                                    }
                                        break;
                                case EffectParameterType.Double: RenderEffect.Parameters[param.Name].SetValue((double)param.Value); break;
                                case EffectParameterType.Int: RenderEffect.Parameters[param.Name].SetValue((int)param.Value); break;
                                case EffectParameterType.Bool: RenderEffect.Parameters[param.Name].SetValue((bool)param.Value); break;
                                case EffectParameterType.UInt: RenderEffect.Parameters[param.Name].SetValue((uint)param.Value); break;
                                case EffectParameterType.UInt8: RenderEffect.Parameters[param.Name].SetValue((uint)param.Value); break;
                            }
                        }
                        catch (Exception)
                        {
                            System.Diagnostics.Debug.WriteLine("Could not convert the parameter type to the corresponding type in the effect.\n " +
                                "Make sure your types in the Effect and in the Parameter are the same!\nYour paramter name: " + param.Name + ". Parameter name on the GPU: " + this.RenderEffect.Name);
                        }
                    }
                    else
                    {//if it is a reference type, EASY, just send it to the GPU
                        RenderEffect.Parameters[param.Name].SetResource(param.Value);
                    }
                }
            }
            //send all the inputs to the GPU
            foreach (KeyValuePair<object, int> kvp in Inputs)
            {
                if (kvp.Key is RenderTarget2D || kvp.Key is Texture2D)
                {
                    string s = kvp.Value.ToString();
                    if (kvp.Value < 0) s = string.Empty;

                    RenderEffect.Parameters["InputTexture" + s].SetResource(kvp.Key);


                }
                else if (kvp.Key is ImageFilter)
                {
                    string s = kvp.Value.ToString();
                    if (kvp.Value < 0) s = string.Empty;
                    RenderEffect.Parameters["InputTexture" + s].SetResource(((ImageFilter)kvp.Key).RenderTarget);
                }
            }

        }

        public override string ToString()
        {
            return RenderEffect.Name;
        }

    }
}
