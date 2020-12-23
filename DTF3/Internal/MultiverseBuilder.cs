using System.Collections.Generic;
using DTF3.Core;
using DTF3.DTFObjects;

namespace DTF3.Internal
{
    internal class MultiverseBuilder
    {
        public ObjectTree ObjectTree { get; }
        public UniverseTree UniverseTree { get; }

        public MultiverseBuilder(Multiverse mVerse)
        {
            ObjectTree = new ObjectTree();
            UniverseTree = new UniverseTree(mVerse, this);
        }

        public ObjectTree.Node RegisterObject(DTFObject obj)
        {
            return new ObjectTree.Node(obj, this);
        }
        
        public void RegisterUniverse(Diff diff)
        {
            
        }
    }
}