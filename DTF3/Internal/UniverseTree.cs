using System.Collections.Generic;
using DTF3.Core;
using DTF3.DTFObjects;
using DTF3.Internal.Interfaces;

namespace DTF3.Internal
{
    internal class UniverseTree
    {
        public Dictionary<Diff, Branch> Branches { get; }
        
        public Multiverse Multiverse { get; }
        
        public Universe RootUniverse { get; }

        public UniverseTree(Multiverse mVerse, MultiverseBuilder mVerseBuilder)
        {
            Branches = new Dictionary<Diff, Branch>();
            Multiverse = mVerse;
            RootUniverse = new Universe(mVerseBuilder.ObjectTree, this);
            Branches[RootUniverse.Diff] = new Branch(RootUniverse.Diff);
        }

        public class Node<T> : IUniverseNode<T> where T : DTFObject
        {
            public static Node<T> Insert(ulong start, Node<T> previous, StateVector<T> vec, Branch newBranch = null)
            {
                Node<T> node = null;
                
                //If this node is part of a new branch, then it is always a new node
                if (newBranch != null)
                {
                    node = new Node<T>
                    {
                        Start = start,
                        Length = 1,
                        Previous = previous,
                        Next = null,
                        StateVector = vec
                    };
                }

                else
                {

                    //If the new state is continuous with the last, just increase the last's length
                    if (previous.Start + previous.Length == start && previous.StateVector.Equals(vec))
                    {
                        previous.Length++;
                        node = previous;
                    }
                    
                    if (previous.Next != null && previous.Next.Start == start + 1 && previous.Next.StateVector.Equals(vec))
                    {
                        if (node == null)
                        {
                            previous.Next.Start--;
                            node = previous.Next;
                        }

                        else
                        {
                            previous.Length += previous.Next.Length;
                            previous.Next = previous.Next.Next;
                        }
                    }

                    if (node != null) return node;
                    
                    //if all else, create new node and insert it
                    //between previous and previous nexts that occur after the new node
                    node = new Node<T>()
                    {
                        Start = start,
                        Length = 1,
                        Previous = previous,
                        StateVector = vec,
                        Next = previous.Next
                    };

                    previous.Next = node;

                    foreach (var next in previous.BranchedNexts)
                    {
                        if (next.Start <= start) continue;
                        
                        previous.BranchedNexts.Remove(next);
                        node.BranchedNexts.Add(next);
                    }
                }

                return node;
            }

            public ulong Start{ get; private set; }
            public ulong Length{ get; private set; }
            public Node<T> Previous{ get; private set;}
            public Node<T> Next { get; private set; }
            public HashSet<Node<T>> BranchedNexts { get; } = new HashSet<Node<T>>();
            public StateVector<T> StateVector{ get; private set; }
            
            private Node(){}
        }
        
        public class Branch
        {
            public readonly Diff Diff;
            public ObjectTree.SuperNode AffectedNode;
            public Dictionary<DTFObject, IUniverseNode<DTFObject>> EndPoints;
            
            internal Branch(Diff diff)
            {
                Diff = diff;
                AffectedNode = diff.AffectedNode;
                
                EndPoints = new Dictionary<DTFObject, IUniverseNode<DTFObject>>();
            }

            public Node<T> GetNodeAtOrBefore<T>(ulong date, T obj) where T : DTFObject
            {
                var current = (Node<T>) EndPoints[obj];

                while (date < current.Start)
                    current = current.Previous;

                return current;
            }
        }
    }
}