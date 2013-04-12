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

        #region GLOBALS

        public string m_Name;
        private object m_Value;
        /// <summary>
        /// if it has been changed we will need to re-send to GPU
        /// </summary>
        public bool HasChanged { get; set; }

        /// <summary>
        /// Get or set the value of the parameter and set it to changed
        /// </summary>
        public object Value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
                HasChanged = true;
            }
        }

        /// <summary>
        /// Get or set the name of the paramter, and set it to changed
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set
            {
                m_Name = value;
                HasChanged = true;
            }
        }

        #endregion

        /// <summary>
        /// Mark the paramter as needs to be sent to GPU when created for the first time
        /// </summary>
        /// <param name="name">Name of the paramter</param>
        /// <param name="value">Value of the paramter</param>
        public Parameter(string name, object value)
        {
            Name = name;
            Value = value;
            HasChanged = true;
        }

    }
}
