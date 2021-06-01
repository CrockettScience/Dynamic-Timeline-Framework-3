using System.Collections.Generic;
using DTF3.DTFObjects;

namespace DTF3.Internal.Interfaces
{
    internal interface IUniverseNode
    {
        
        ulong Start{ get; set; }
        ulong Length{ get; set; }

        IUniverseNode Previous { get; set; }
        IStateVector StateVector { get; set; }
        IUniverseNode Next { get; set; }
        HashSet<IUniverseNode> BranchedNexts { get; }
    }
}