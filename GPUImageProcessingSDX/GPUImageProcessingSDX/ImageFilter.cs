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
        private Effect m_Effect;
        private RenderTarget2D m_RenderTarget;
        public List<ImageFilter> Children;
        public List<ImageFilter> Parents;
        public List<Parameter> Parameters;
        public Dictionary<object, int> Inputs;
        public bool NeedRender = true;
        public string Path = string.Empty;
        public bool InitialRead = false;
        public WriteableBitmap NewImg;
        public List<int> OverwriteWith;
        

        public Effect RenderEffect
        {
            get { return m_Effect; }
            set { m_Effect = value; }
        }

        public RenderTarget2D RenderTarget
        {
            get { return m_RenderTarget; }
            set { m_RenderTarget = value; }
        }



        public ImageFilter(string p = "", params Parameter[] list)
        {

            Children = new List<ImageFilter>();
            Parents = new List<ImageFilter>();
            Inputs = new Dictionary<object, int>();

            OverwriteWith = new List<int>();

            if (p != "")
            {
                Path = p;
            }

            Parameters = new List<Parameter>();

            foreach (Parameter par in list)
            {
                Parameters.Add(par);
            }
        }
        
        public void AddInput(ImageFilter imfil, int num = -1)
        {
            Parents.Add(imfil);
            imfil.Children.Add(this);
            Inputs.Add(imfil, num);
        }

        public void AddInput(RenderTarget2D rt, int num = -1)
        {
            Inputs.Add(rt, num);
        }

        public void AddInput(Texture2D tex, int num = -1)
        {
            Inputs.Add(tex, num);
        }

        /// <summary>
        /// update a parameter value
        /// </summary>
        /// <param name="name">The parameter to update</param>
        /// <param name="value">The value to change to</param>
        /// <returns>true if the parameter is found, and updated sucessfully.</returns>
        public bool UpdateParameter(string name, object value)
        {
            bool success = false;

            foreach (Parameter p in Parameters)
            {
                if (p.Name == name)
                {
                    p.Value = value;
                    success = true;
                    p.HasChanged = true;
                    GPUImageGame.NeedsRender = true;
                    break;
                }
            }

            return success;
        }

        public virtual void SendParametersToGPU()
        {
            foreach (Parameter param in Parameters)
            {
                if (param.HasChanged)
                {
                    if (RenderEffect.Parameters[param.Name].IsValueType)
                    {
                        EffectParameter p = RenderEffect.Parameters[param.Name];
                        EffectParameterType type = p.ParameterType;
                        
                        
                        try
                        {
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
                                "Make sure your types in the Effect and in the Parameter are the same! ......." + param.Name + "....." + this.RenderEffect.Name);
                        }
                    }
                    else
                    {
                        RenderEffect.Parameters[param.Name].SetResource(param.Value);
                    }
                }
            }

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
