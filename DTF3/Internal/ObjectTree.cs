using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DTF3.Core;
using DTF3.DTFObjects;

namespace DTF3.Internal
{
    internal class ObjectTree
    {
        public SuperNode Root { get; }
        
        public ObjectTree()
        {
            //Set up the root node, which has no objects in it
            Root = new SuperNode();
        }

        public class Node
        {
            public Node Parent{ get; }
            
            public DTFObject DTFObject{ get; }
            
            public SuperNode SuperNode{ get; }
            
            public HashSet<Node> LateralNodes{ get; }

            public Node(DTFObject obj, MultiverseBuilder builder)
            {
                LateralNodes = new HashSet<Node>();
                DTFObject = obj;

                if (obj.HasLateralObjects)
                {
                    SuperNode = obj.GetLateralObjects.First().ObjectNode.SuperNode;
                    
                    foreach (var lat in obj.GetLateralObjects)
                        LateralNodes.Add(lat.ObjectNode);
                }
                
                else SuperNode = new SuperNode(this, builder);
                
                
                Parent = obj.HasParent ? obj.Parent.ObjectNode : null;
            }

        }

        public class SuperNode
        {
            private HashSet<Diff> _diffs;
            
            public HashSet<Node> Nodes{ get; }

            public HashSet<Diff> Diffs
            {
                get
                {
                    if (SuperParent == null)
                        return _diffs;
                    
                    var parentDiffs = SuperParent.Diffs;
                    
                    foreach (var diff in parentDiffs)
                        if(!_diffs.Contains(diff))
                            _diffs.Add(diff);

                    return _diffs;
                }
            }

            public SuperNode SuperParent{ get; }

            public SuperNode(Node subNode, MultiverseBuilder builder)
            {
                var obj = subNode.DTFObject;
                
                Nodes = new HashSet<Node>(new []{subNode});
                _diffs = new HashSet<Diff>();

                if (!obj.HasParent)
                    SuperParent = builder.ObjectTree.Root;

                else
                    SuperParent = obj.Parent.ObjectNode.SuperNode;
            }

            public SuperNode()
            {
                Nodes = null;
                _diffs = new HashSet<Diff>();
                SuperParent = null;
            }
        }
    }
}