using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using DTF3.Core;
using DTF3.DTFObjects;
using DTF3.Internal.Interfaces;
using MathNet.Numerics.LinearAlgebra;

namespace DTF3.Internal
{
    internal class UniverseTree
    {
        public Dictionary<Diff, Branch> Branches { get; }
        
        public Multiverse Multiverse { get; }
        
        public Universe RootUniverse { get; }

        public readonly Random Random;

        public UniverseTree(Multiverse mVerse, MultiverseBuilder mVerseBuilder)
        {
            Branches = new Dictionary<Diff, Branch>();
            Multiverse = mVerse;
            RootUniverse = new Universe(mVerseBuilder.ObjectTree, this);
            Branches[RootUniverse.Diff] = new Branch(RootUniverse, null);
            
            Random = new Random();
        }

        public void Register(Universe universe)
        {
            //Create branch
            var newBranch = new Branch(universe, Branches[universe.Diff.Parent]);
            Branches[universe.Diff] = newBranch;
            
            //Register catalyzing object
            Register(universe.Diff.Catalyst, universe);
            
        }

        public void Register<T>(T obj, Universe universe = null) where T: DTFObject
        {
            var branch = universe == null ? Branches[RootUniverse.Diff] : Branches[universe.Diff];
            
            branch.Register(obj);
        }

        public class Node : IUniverseNode
        {
            public static IUniverseNode Insert(ulong start, IUniverseNode previous, IStateVector vec, bool onBranchEdge, IUniverseNode branchEdgeNext = null)
            {
                IUniverseNode node = null;
                
                //If this node is part of a new branch, then it is always a new node
                if (onBranchEdge)
                {
                    node = new Node
                    {
                        Start = start,
                        Length = 1,
                        Previous = previous,
                        Next = branchEdgeNext,
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
                    
                    //If the new state is continuous with the next
                    if (previous.Next != null && previous.Next.Start == start + 1 && previous.Next.StateVector.Equals(vec))
                    {
                        //And the node hasn't already been assigned to the last with increased length, decrease the next's start and increase the next's length
                        if (node == null)
                        {
                            previous.Next.Start--;
                            previous.Next.Length++;
                            node = previous.Next;
                        }

                        //Else, just extend the previous across the next and replace the next with one big node
                        else
                        {
                            previous.Length += previous.Next.Length;
                            previous.Next = previous.Next.Next;
                        }
                    }

                    if (node != null) return node;
                    
                    //if all else, create new node and insert it
                    //between previous and it's nexts that occur after the new node
                    node = new Node()
                    {
                        Start = start,
                        Length = 1,
                        Previous = previous,
                        StateVector = vec,
                        Next = previous.Next
                    };

                    if(previous.Next != null)
                        previous.Next.Previous = node;
                    
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

            public ulong Start{ get; set; }
            public ulong Length{ get; set; }
            public IUniverseNode Previous{ get; set;}
            public IUniverseNode Next { get; set; }
            public HashSet<IUniverseNode> BranchedNexts { get; } = new HashSet<IUniverseNode>();
            public IStateVector StateVector{ get; set; }
            
            private Node(){}
        }
        
        public class Branch
        {
            private readonly Universe _universe;

            private readonly Dictionary<DTFObject, IUniverseNode> _endPoints;
            
            public Branch Parent { get; }

            public ulong Date => _universe.Diff.Date;
            
            internal Branch(Universe universe, Branch parent)
            {
                _universe = universe;
                Parent = parent;

                _endPoints = new Dictionary<DTFObject, IUniverseNode>();
            }

            internal void Register<T>(T obj) where T: DTFObject
            {
                if (obj.ObjectNode == null) 
                    //The object is in the process of being registered and needs to insert an endpoint on the root branch
                    _endPoints[obj] = Node.Insert(0, null, StateVector.WildCardState(obj.Data), true);


                else _endPoints[obj] = InsertState(0, obj, StateVector.WildCardState(obj.Data));
            }

            internal IUniverseNode InsertState(ulong date, DTFObject obj, IStateVector vec)
            {
                if (date < Date)
                {
                    //Do this on parent branch
                    Parent.InsertState(date, obj, vec);
                }
                
                var previous =  _endPoints[obj];
                var next = (IUniverseNode) null;

                while (date < previous.Start)
                {
                    next = previous;
                    previous = previous.Previous;
                }


                if (previous.Start + previous.Length > date)
                    return previous;
                
                var onBranchEdge = next != null && next != previous.Next;

                var node =  Node.Insert(date, previous, vec, onBranchEdge, next);

                //Update the endpoint if needed
                if (!_endPoints.TryGetValue(obj, out var endpoint) || date >= endpoint.Start + endpoint.Length)
                    _endPoints[obj] = node;

                return node;

            }

            internal IStateVector Forecast(ulong targetDate, DTFObject obj)
            {
                if (!IsRegistered(obj))
                    return Parent.Forecast(targetDate, obj);
                
                var previous = _endPoints[obj];
                while (targetDate < previous.Start)
                {
                    previous = previous.Previous;
                }
                
                //Return current if it's state spans over the date we're targeting
                var lastConfirmedDate = previous.Start + previous.Length - 1;
                if (lastConfirmedDate >= targetDate)
                    return previous.StateVector;

                //Get forecast from past state
                var forecastFromPast = previous.StateVector.GetTransition(targetDate - lastConfirmedDate);
                
                var forecasts = new List<Vector<double>>();
                forecasts.Add(forecastFromPast.Vector);
                
                //Get forecast from parent states
                var data = obj.Data;
                if (obj.HasParent)
                {
                    var parentForecast = Forecast(targetDate, obj.Parent);
                    
                    var translationMatrix = data.TranslationMatrix(data.MetaData.ParentKey);
                    
                    forecasts.Add(Vector<double>.Build.SparseOfVector(parentForecast.Vector * translationMatrix));
                }
                
                //Get forecasts from lateral objects in supernode
                foreach (var lateralKey in obj.GetLateralKeys)
                {
                    var lateralForecast = Forecast(targetDate, obj.GetLateralObject(lateralKey));
                    var translationMatrix = data.TranslationMatrix(lateralKey);
                    
                    forecasts.Add(Vector<double>.Build.SparseOfVector(lateralForecast.Vector * translationMatrix));
                }
                
                //Combine all the forecasts together and return
                var finalForecastVector = Vector<double>.Build.Sparse(forecasts[0].Count);
                foreach (var forecast in forecasts)
                {
                    finalForecastVector += forecast;
                }
                
                finalForecastVector /= forecasts.Count;
                var forecastStateVector = new StateVector(finalForecastVector, obj.Data);
                
                //Mask out impossibilities
                var futureMask = FutureMask(targetDate, obj);
                forecastStateVector = (StateVector) forecastStateVector.SafeMask(this, targetDate, obj, futureMask);

                return forecastStateVector;
            }

            internal IStateVector Guess(ulong targetDate, DTFObject obj)
            {
                if (!IsRegistered(obj))
                    return Parent.Guess(targetDate, obj);
                
                var previous = _endPoints[obj];
                while (targetDate < previous.Start)
                {
                    previous = previous.Previous;
                }
                
                //Get future mask
                var mask = FutureMask(targetDate, obj);
                
                //Get past mask
                var pastMask = new PossibilityMask(previous.StateVector);
                pastMask.ProjectForward();
                
                //Combine and produce balanced stateVector based on remaining possibilities
                mask.And(pastMask);
                var vector = StateVector.BalancedState(obj.Data);

                return vector * mask;
            }

            public PossibilityMask FutureMask<T>(ulong targetDate, T obj) where T : DTFObject
            {
                if (!IsRegistered(obj))
                    return Parent.FutureMask(targetDate, obj);
                
                var previous = _endPoints[obj];
                while (targetDate < previous.Start)
                {
                    previous = previous.Previous;
                }
                
                //Combine masks from all future states
                var futureMask = PossibilityMask.GetOpenMask(obj.Data);

                if (previous.Next != null)
                {
                    var mask = new PossibilityMask(previous.Next.StateVector);
                    mask.ProjectBackward();
                    futureMask.And(mask);
                }

                foreach (var next in previous.BranchedNexts)
                {
                    var mask = new PossibilityMask(next.StateVector);
                    mask.ProjectBackward();
                    futureMask.And(mask);
                }
                
                return futureMask;
            }

            private bool IsRegistered(DTFObject obj)
            {
                
                //If it is registered, return true, else, check if it SHOULD be registered
                if (_endPoints.ContainsKey(obj)) return true;

                if (obj.ObjectNode.SuperNode.IsAffectedBy(_universe.Diff.AffectedNode))
                {
                    //Register object and return true
                    Register(obj);
                    return true;
                }

                return false;
            }

            public IStateVector GetStateAtOrBefore(ulong targetDate, DTFObject obj)
            {
                if (targetDate < Date || !IsRegistered(obj))
                {
                    return Parent.GetStateAtOrBefore(targetDate, obj);
                }
                
                var previous = _endPoints[obj];
                while (targetDate < previous.Start)
                {
                    previous = previous.Previous;
                }

                return previous.StateVector;
            }
        }
    }
}