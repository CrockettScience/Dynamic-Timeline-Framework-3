using DTF3.DTFObjects;
using DTF3.Internal;
using DTF3.Internal.Interfaces;

namespace DTF3.Core
{
    public class Diff
    {
        public Diff Parent { get; }
        public ulong Date { get; }
        internal ObjectTree.SuperNode AffectedNode { get; }
        internal UniverseTree UniverseTree { get; }
        
        public DTFObject Catalyst { get; }

        private readonly IPosition<DTFObject> _catalyzingState;

        internal Diff(Universe parentUniverse, ulong date, DTFObject catalyst, IPosition<DTFObject> catalyzingState)
        {
            Parent = parentUniverse.Diff;
            UniverseTree = parentUniverse.UniverseTree;
            Date = date;
            Catalyst = catalyst;
            _catalyzingState = catalyzingState;

            //Todo - Should test to see if parent supernode is affected, recursively
            AffectedNode = Catalyst.ObjectNode.SuperNode;

        }

        internal Diff(ObjectTree.SuperNode rootNode, UniverseTree tree)
        {
            Parent = null;
            UniverseTree = tree;
            Date = 0;
            AffectedNode = rootNode;
            Catalyst = null;
        }

        public Position<T> GetCatalyzingState<T>() where T : DTFObject
        {
            return (Position<T>) _catalyzingState;
        }

        public bool IsAffected(DTFObject obj)
        {
            var current = obj.ObjectNode.SuperNode;

            while (current != AffectedNode)
            {
                current = current.SuperParent;

                if (current == null)
                    return false;
            }

            return true;

        }
    }
}