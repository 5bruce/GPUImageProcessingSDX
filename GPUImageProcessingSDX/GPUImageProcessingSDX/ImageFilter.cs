using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUImageProcessingSDX
{
    class ImageFilter
    {
        private Effect m_Effect;
        private RenderTarget2D m_RenderTarget;
        private ImageFilter m_NextFilter;
        List<Parameter> Parameters;

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

        public ImageFilter NextFilter
        {
            get { return m_NextFilter; }
            set { m_NextFilter = value; }
        }

        public ImageFilter(Effect eff, RenderTarget2D rt, params Parameter[] list)
        {
            RenderEffect = eff;
            RenderTarget = rt;

            Parameters = new List<Parameter>();

            foreach (Parameter p in list)
            {
                Parameters.Add(p);
            }

        }

        public virtual void UpdateParameters()
        {
            foreach (Parameter param in Parameters)
            {
                if (param.HasChanged)
                {
                    if (RenderEffect.Parameters[param.Name].IsValueType)
                    {
                        EffectParameterType type = RenderEffect.Parameters[param.Name].ParameterType;
                        try
                        {
                            switch (type)
                            {
                                case EffectParameterType.Float: RenderEffect.Parameters[param.Name].SetValue((float)param.Value); break;
                                case EffectParameterType.Double: RenderEffect.Parameters[param.Name].SetValue((double)param.Value); break;
                                case EffectParameterType.Int: RenderEffect.Parameters[param.Name].SetValue((int)param.Value); break;
                                case EffectParameterType.Bool: RenderEffect.Parameters[param.Name].SetValue((bool)param.Value); break;
                                case EffectParameterType.UInt: RenderEffect.Parameters[param.Name].SetValue((uint)param.Value); break;
                                case EffectParameterType.UInt8: RenderEffect.Parameters[param.Name].SetValue((uint)param.Value); break;
                            }
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine("Could not convert the parameter type to the corresponding type in the effect.\n " +
                                "Make sure your types in the Effect and in the Parameter are the same!");
                        }
                    }
                    else
                    {
                        RenderEffect.Parameters[param.Name].SetResource(param.Value);
                    }

                    param.HasChanged = false;

                }
            }
        }

        public override string ToString()
        {
            return RenderEffect.Name;
        }

    }
}
