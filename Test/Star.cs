using DTF3.Core;
using DTF3.DTFObjects;

namespace Test
{
    [DTFObject("Star")]
    public class Star : DTFObject
    {
        public Star(Galaxy parent)
        {
            SetParent(parent);
            
            Register(Program.Multiverse);
        }
    }
}