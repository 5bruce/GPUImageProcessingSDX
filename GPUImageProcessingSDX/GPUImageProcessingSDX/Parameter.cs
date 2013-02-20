using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUImageProcessingSDX
{
    class Parameter
    {
        public string m_Name;
        private object m_Value;
        public bool HasChanged { get; set; }
        public bool Resource { get; set; }

        public object Value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
                HasChanged = true;
            }
        }

        public string Name
        {
            get { return m_Name; }
            set
            {
                m_Name = value;
                HasChanged = true;
            }
        }

        public Parameter(string name, object value)
        {
            Name = name;
            Value = value;
            HasChanged = true;
        }


    }
}
