using System;
using DTF3.Core;
using DTF3.DTFObjects;

namespace DTF3.Exception
{
    public class DTFInvalidCollapseException : System.Exception
    {
        public DTFInvalidCollapseException(Type DTFObjectType)
        {
            Message = "Asserted an invalid collapse for a(n) " + DTFObjectType + ". Asserted Wavefunction must be " +
                      "coherent with past wavefunction in the timeline";
        }

        public override string Message { get; }
    }
}