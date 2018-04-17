using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keysight.S8901A.Common
{
    /// <summary>
    /// Indicates whether the class(TapStep) or field(public propertis of the TapStep)
    /// is visible from TCF generator's point of view
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TcfVisibleAttribute : Attribute
    {
        public TcfVisibleAttribute()
        {
            TcfVisible = true;
        }

        public bool TcfVisible { get; set; }
    }
}

