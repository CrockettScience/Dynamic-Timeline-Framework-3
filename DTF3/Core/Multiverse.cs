using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly Dictionary<Type, DTFObjectData> _dtfObjectData = new Dictionary<Type, DTFObjectData>();
        
        private readonly Dictionary<string, Type> _dtfObjectTypes = new Dictionary<string, Type>();

        private readonly MultiverseBuilder _builder;
        
        private readonly Dictionary<Diff, Universe> _multiverse;

        public Universe RootUniverse => _builder.UniverseTree.RootUniverse;

        public Universe this[Diff d] => d == RootUniverse.Diff ? RootUniverse : _multiverse[d];

        public Multiverse(string jsonPath)
        {
            //Compile DTFObjectData
            var data = JsonConvert.DeserializeObject<List<DTFObjectData.ObjectMetadata>>(File.ReadAllText(jsonPath));
            
            var dataMap = new Dictionary<string, DTFObjectData>();
            
            foreach (var objectData in data)
            {
                dataMap[objectData.TypeName] = new DTFObjectData(this, objectData);
                
            }
            
            var types = Assembly.GetCallingAssembly().GetTypes();

            foreach (var type in types)
            {
                if (!type.IsSubclassOf(typeof(DTFObject))) continue;
                if (!(type.GetCustomAttribute(typeof(DTFObjectAttribute)) is DTFObjectAttribute attr))
                    throw new DTFException(type.Name + " is assignable from " + typeof(DTFObject) +
                                           " but does not have a " + typeof(DTFObjectAttribute) +
                                           " defined.");

                _dtfObjectData[type] = dataMap[attr.TypeName];
                _dtfObjectTypes[attr.TypeName] = type;

            }
            
            //Initialize Multiverse Structure
            _multiverse = new Dictionary<Diff, Universe>();
            _builder = new MultiverseBuilder(this);
        }

        internal ObjectTree.Node RegisterObject<T>(T obj) where T: DTFObject
        {
            return _builder.RegisterObject(obj);
        }

        internal DTFObjectData GetObjectData(Type dtfObjectType)
        {
            return _dtfObjectData[dtfObjectType];
        }

        internal void AddUniverse(Universe universe)
        {
            var diff = universe.Diff;
            _multiverse[diff] = universe;
            diff.AffectedNode.Diffs.Add(diff);
            _builder.RegisterUniverse(universe);
        }

        internal class DTFObjectData
        {
            public Multiverse Multiverse;

            public ObjectMetadata MetaData;

            public DTFObjectData(Multiverse multiverse, ObjectMetadata metaData)
            {
                Multiverse = multiverse;
                MetaData = metaData;
            }
            
            /// <summary>
            /// Multiply this matrix by a vector from the object denoted by keyName to get the translation forecast vector for this object
            /// </summary>
            public Matrix<double> TranslationMatrix(string keyName)
            {
                var latObj = MetaData.LateralDictionary[keyName];
                var matrix = latObj.TranslationMatrix;
                if (matrix == null)
                {
                    //If it doesn't exist, create it
                    var otherObjectData = Multiverse._dtfObjectData[Multiverse._dtfObjectTypes[latObj.TypeName]];
                    matrix = Matrix.Build.Dense(otherObjectData.MetaData.States.Count, MetaData.States.Count);

                    //Construct the translation matrix
                    foreach (var rowIndex in otherObjectData.MetaData.Indices)
                    {
                        var row = rowIndex.Value;

                        foreach (var colIndex in MetaData.Indices)
                        {
                            var col = colIndex.Value;

                            matrix[row, col] = MetaData.States[col].ConstraintMap[keyName][rowIndex.Key];
                        }
                    }

                    latObj.TranslationMatrix = matrix;
                }

                return matrix;
            }

            public DTFObjectData GetLateralData(string keyName)
            {
                return Multiverse._dtfObjectData[Multiverse._dtfObjectTypes[MetaData.LateralDictionary[keyName].TypeName]];
            }

            internal class ObjectMetadata
            {

                /// <summary>
                /// The specific states that an object of this type can be in.
                /// </summary>
                private List<State> _states;

                /// <summary>
                /// A map of indices that map state names to the index they correspond to in transition and translation matrices.
                /// </summary>
                public readonly Dictionary<string, int> Indices = new Dictionary<string, int>();

                /// <summary>
                /// A map of lateral objects that map their key to the lateral data. 
                /// </summary>
                public readonly Dictionary<string, LateralObject> LateralDictionary =
                    new Dictionary<string, LateralObject>();

                public string StateName(int i) => _states[i].StateName;

                public int TransitionIndex(string stateName) => Indices[stateName];

                public Matrix<double> TransitionMatrix;

                [JsonProperty("type_name")] 
                public string TypeName { get; set; }

                [JsonProperty("parent_key_name")] 
                public string ParentKey { get; set; }

                [JsonProperty("lateral_objects")]
                public List<LateralObject> LateralObjects
                {
                    set
                    {
                        foreach (var lateralObject in value)
                        {
                            LateralDictionary[lateralObject.KeyName] = lateralObject;
                        }
                    }
                }

                [JsonProperty("states")]
                public List<State> States
                {
                    get => _states;
                    set
                    {
                        //Set up the transition matrix
                        TransitionMatrix = Matrix.Build.Dense(value.Count, value.Count);

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
                                    total += transition.StochasticProbability;

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
                                        trueProbability =
                                            transitions[transitionState.StateName].StochasticProbability *
                                            (1.0 - probabilityVariable);

                                    TransitionMatrix[i, j] = trueProbability;
                                }
                            }

                            Indices[state.StateName] = i;
                        }
                        
                        //Calculate the stochastic probabilities for each state's constraints
                        foreach (var lateralKey in LateralDictionary.Keys)
                        {
                            var totals = new Dictionary<string, double>();
                            foreach (var state in value)
                            {
                                var constraint = state.ConstraintMap[lateralKey];

                                foreach (var probabilityPair in constraint.ProbabilityMap)
                                {
                                    if (totals.ContainsKey(probabilityPair.Key))
                                        totals[probabilityPair.Key] += probabilityPair.Value.Probability;

                                    else totals[probabilityPair.Key] = probabilityPair.Value.Probability;
                                }
                            }

                            foreach (var state in value)
                            {
                                var constraint = state.ConstraintMap[lateralKey];
                                
                                foreach (var probabilityPair in constraint.ProbabilityMap)
                                {
                                    probabilityPair.Value.StochasticProbability = probabilityPair.Value.Probability / totals[probabilityPair.Key];
                                }
                            }
                        }

                        _states = value;
                    }
                }

                public class LateralObject
                {

                    public Matrix<double> TranslationMatrix { get; set; }

                    [JsonProperty("type_name")] public string TypeName { get; set; }

                    [JsonProperty("key_name")] public string KeyName { get; set; }

                }

                public class State
                {
                    #region STATIC

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

                    #endregion

                    public Dictionary<string, Constraint> ConstraintMap = new Dictionary<string, Constraint>();
                    private List<StateProbability> _transitions;

                    public ulong TargetLength;

                    [JsonProperty("state_name")] public string StateName { get; set; }

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
                                            throw new DTFObjectDataException(
                                                "Unrecognized Token \"" + token + "\" in target length of state " +
                                                StateName);
                                    }
                                }
                            }

                            val += subVal;

                            if (val > MAX)
                                throw new DTFObjectDataException(
                                    "Maximum target length exceeded. Must not exceed 10,950,000,000,000,000 (30 Trillion Years)");
                            TargetLength = val;
                        }
                    }

                    [JsonProperty("transitions_to")]
                    public List<StateProbability> Transitions
                    {
                        get => _transitions;
                        set
                        { 
                            _transitions = value;

                            //Calculate the stochastic probabilities
                            var total = value.Sum(prob => prob.Probability);

                            foreach (var stateProbability in value)
                            {
                                stateProbability.StochasticProbability = stateProbability.Probability / total;
                            }
                        } 
                    }

                    [JsonProperty("constraints")]
                    public List<Constraint> Constraints
                    {
                        set
                        {
                            foreach (var constraint in value)
                            {
                                ConstraintMap[constraint.KeyName] = constraint;
                            }
                        }
                    }

                    public class StateProbability
                    {
                        private bool _hasCustomProbability;

                        private double _probability;

                        public double StochasticProbability { get; set; }

                        [JsonProperty("state_name")] public string StateName { get; set; }

                        [JsonProperty("probability")]
                        public double Probability
                        {
                            get => _hasCustomProbability ? _probability : 1;
                            set
                            {
                                _hasCustomProbability = true;
                                _probability = value;
                            }
                        }
                    }

                    public class Constraint
                    {

                        public readonly Dictionary<string, StateProbability> ProbabilityMap =
                            new Dictionary<string, StateProbability>();

                        /// <summary>
                        /// A constraint describes the chances that the given state occurs with the states of the lateral object
                        /// </summary>
                        /// <param name="stateName">Name of the state of the other type this probability describes</param>
                        public double this[string stateName]
                        {
                            get
                            {
                                if (!ProbabilityMap.ContainsKey(stateName))
                                    return 0.0;

                                var prob = ProbabilityMap[stateName];
                                return prob.StochasticProbability;
                            }
                        }

                        [JsonProperty("key_name")] public string KeyName { get; set; }

                        [JsonProperty("constrained_states")]
                        public List<StateProbability> ConstrainedStates
                        {
                            set
                            {
                                foreach (var prob in value)
                                {
                                    ProbabilityMap[prob.StateName] = prob;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}