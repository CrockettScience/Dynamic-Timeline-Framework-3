using System;
using DTF3.DTFObjects;
using DTF3.Internal;

namespace DTF3.Core
{
    public class Universe
    {
        internal UniverseTree UniverseTree => Diff.UniverseTree;
        public Diff Diff { get; }
        
        public Universe(Diff diff)
        {
            Diff = diff;
            UniverseTree.Multiverse.AddUniverse(this);
        }

        internal Universe(ObjectTree oTree, UniverseTree uTree)
        {
            var oRootNode = oTree.Root;
            Diff = new Diff(oRootNode, uTree);
            oRootNode.Diffs.Add(Diff);
        }

        public Continuity GetContinuity(DTFObject obj)
        {
            return new Continuity(obj, this);
        }
    }
}