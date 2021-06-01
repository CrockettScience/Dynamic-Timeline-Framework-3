using DTF3.Core;
using DTF3.DTFObjects;

namespace Test
{
    [DTFObject("Star")]
    public class Star : DTFObject
    {
        public Star(Multiverse mVerse, Galaxy parent) : base(mVerse)
        {
            SetParent(parent);
            
            Register(this);
        }
    }
}