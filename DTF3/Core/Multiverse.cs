using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DTF3.DTFObjects;
using DTF3.Exception;
using DTF3.Internal;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Newtonsoft.Json;

namespace DTF3.Core
{
    public class Multiverse
    {
        internal static readonly Dictionary<Type, DTFObjectData> DTF_OBJECT_DATA = new Dictionary<Type, DTFObjectData>();

        private readonly MultiverseBuilder _builder;
        
        private readonly Dictionary<Diff, Universe> _multiverse;

        public Universe RootUniverse => _builder.UniverseTree.RootUniverse;

        public Universe this[Diff d] => d == RootUniverse.Diff ? RootUniverse : _multiverse[d];

        public Multiverse(string jsonPath)
        {
            //Compile DTFObjectData
            var data = JsonConvert.DeserializeObject<List<DTFObjectData>>(File.ReadAllText(jsonPath));

            var objectDataNames = new Dictionary<string, DTFObjectData>();
            
            foreach (var objectData in data)
            {
                objectDataNames[objectData.TypeName] = objectData;
                
            }
            
            var types = Assembly.GetCallingAssembly().GetTypes();

            foreach (var type in types)
            {
                if (!type.IsSubclassOf(typeof(DTFObject))) continue;
                if (!(type.GetCustomAttribute(typeof(DTFObjectAttribute)) is DTFObjectAttribute attr))
                    throw new DTFException(type.Name + " is assignable from " + typeof(DTFObject) +
                                           " but does not have a " + typeof(DTFObjectAttribute) +
                                           " defined.");

                DTF_OBJECT_DATA[type] = objectDataNames[attr.TypeName];

            }
            
            //Initialize Multiverse Structure
            _multiverse = new Dictionary<Diff, Universe>();
            _builder = new MultiverseBuilder(this);
        }

        internal (DTFObjectData, ObjectTree.Node) RegisterObject(DTFObject obj)
        {
            return (DTF_OBJECT_DATA[obj.GetType()], _builder.RegisterObject(obj));
        }

        internal void AddUniverse(Diff diff, Universe universe)
        {
            _multiverse[diff] = universe;
            diff.AffectedNode.Diffs.Add(diff);
            _builder.RegisterUniverse(diff);
        }

        internal class DTFObjectData
        {
            private List<State> _states;
            private Dictionary<string, int> _indices;

            public Matrix<double> TransitionMatrix;

            public string GetStateName(int i) => _states[i].StateName;
            public int GetTransitionIndex(string stateName) => _indices[stateName];

            [JsonProperty("type_name")]
            public string TypeName { get; set; }
            
            [JsonProperty("parent_key_name")]
            public string ParentKey { get; set; }

            [JsonProperty("lateral_objects")]
            public List<LateralObject> LateralObjects { get; set; }

            [JsonProperty("states")]
            public List<State> States
            {
                get => _states;
                set
                {
                    //Set up the transition matrix
                    TransitionMatrix = Matrix.Build.Dense(value.Count, value.Count);
                    
                    //Set up index map
                    _indices = new Dictionary<string, int>(value.Count);

                    //Create the transition matrix
                    for (var i = 0; i < value.Count; i++)
                    {
                        var state = value[i];
                        
                        //If no transitions ae defined, then the only transition happens with itself
                        if (state.Transitions.Count == 0)
                            TransitionMatrix[i, i] = 1;

                        else
                        {
                            //All of the state's transition probabilities must add up to one
                            var total = 0.0;
                            var transitions = new Dictionary<string, State.StateProbability>();
                            foreach (var transition in state.Transitions)
                            {
                                total += transition.Probability;

                                transitions[transition.StateName] = transition;
                            }

                            if (Math.Abs(total - 1) > 0.00000001)
                                throw new DTFObjectDataException(
                                    "State " + state.StateName + " of object type " + TypeName +
                                    " has transition states with probabilities that do not add up to 1");
                            
                            var probabilityVariable = Math.Pow(0.5, 1.0 / state.TargetLength); 
                            for (var j = 0; j < value.Count; j++)
                            {
                                var transitionState = value[j];
                                
                                double trueProbability;
                                
                                //Case 1: State transition to itself
                                if (i == j) trueProbability = probabilityVariable;
                                
                                //Case 2: State transition untransitionable
                                else if (!transitions.ContainsKey(transitionState.StateName)) trueProbability = 0;

                                //Case 3: State transition is transitionable
                                else
                                    trueProbability = transitions[transitionState.StateName].Probability * (1.0 - probabilityVariable);

                                TransitionMatrix[i, j] = trueProbability;
                            }
                        }
                        
                        _indices[state.StateName] = i;
                    }

                    _states = value;
                } 
            }

            public class LateralObject
            {
                [JsonProperty("type_name")]
                public string TypeName { get; set; }
                
                [JsonProperty("key_name")]
                public string KeyName { get; set; }
                
            }

            public class State
            {
                private const ulong DAY = 1;
                private const ulong WEEK = 7;
                private const ulong MONTH = 30;
                private const ulong YEAR = 365;

                private const ulong HUNDRED = 100;
                private const ulong THOUSAND = 1_000;
                private const ulong MILLION = 1_000_000;
                private const ulong BILLION = 1_000_000_000;
                private const ulong TRILLION = 1_000_000_000_000;

                private const ulong MAX = 10_950_000_000_000_000;
                
                public ulong TargetLength;

                [JsonProperty("state_name")]
                public string StateName { get; set; }

                [JsonProperty("target_length")]
                public string TargetLengthString
                {
                    set
                    {
                        var val = 0UL;
                        var subVal = 0UL;
                        foreach (var token in value.Split())
                        {
                            if (ulong.TryParse(token, out var parsed))
                            {
                                val += subVal;
                                subVal = parsed;
                            }

                            else
                            {
                                //Didn't parse, better be one of the token keywords or error will be thrown
                                switch (token.ToLower())
                                {
                                    case "days":
                                        subVal *= DAY;
                                        break;
                                    
                                    case "weeks":
                                        subVal *= WEEK;
                                        break;
                                    
                                    case "months":
                                        subVal *= MONTH;
                                        break;
                                    
                                    case "years":
                                        subVal *= YEAR;
                                        break;
                                    
                                    case "hundred":
                                        subVal *= HUNDRED;
                                        break;
                                    
                                    case "thousand":
                                        subVal *= THOUSAND;
                                        break;
                                    
                                    case "million":
                                        subVal *= MILLION;
                                        break;
                                    
                                    case "billion":
                                        subVal *= BILLION;
                                        break;
                                    
                                    case "trillion":
                                        subVal *= TRILLION;
                                        break;
                                    
                                    default:
                                        throw new DTFObjectDataException("Unrecognized Token \"" + token + "\" in target length of state " + StateName);
                                }
                            }
                        }

                        val += subVal;
                        
                        if(val > MAX)
                            throw new DTFObjectDataException("Maximum target length exceeded. Must not exceed 10,950,000,000,000,000 (30 Trillion Years)");
                        TargetLength = val;
                    }
                }

                [JsonProperty("transitions_to")]
                public List<StateProbability> Transitions { get; set; }

                [JsonProperty("constraints")]
                public List<Constraint> Constraints { get; set; }

                public class StateProbability
                {
                    
                    [JsonProperty("state_name")]
                    public string StateName { get; set; }
                    
                    [JsonProperty("probability")]
                    public double Probability { get; set; }
                    
                }

                public class Constraint
                {
                    private List<StateProbability> _constrainedStates;

                    [JsonProperty("key_name")]
                    public string KeyName { get; set; }

                    [JsonProperty("constrained_states")]
                    public List<StateProbability> ConstrainedStates
                    {
                        get => _constrainedStates;
                        set
                        {
                            //Todo - set up translation matrix. make sure to somehow make the matrix 
                        }
                    }
                }
            }
        }
    }
}