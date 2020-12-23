using DTF3.DTFObjects;

namespace DTF3.Core
{
    public class Continuity<T> where T : DTFObject
    {
        private readonly T _obj;
        private readonly Universe _universe;

        internal Continuity(T obj, Universe universe)
        {
            _obj = obj;
            _universe = universe;
        }

        
        public void Measure(ulong date)
        {
            
        }
        
        public bool Assert(ulong date, Position<T> pose, out Diff diff)
        {
            diff = null;
            return false;
        }
    }
}