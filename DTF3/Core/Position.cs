using System.Collections.Generic;
using DTF3.DTFObjects;
using DTF3.Internal;
using DTF3.Internal.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Storage;

namespace DTF3.Core
{
    public class Position<T> : IPosition<T> where T : DTFObject
    {
        internal readonly Multiverse.DTFObjectData Data;
        private readonly string _stateName;

        public Position(string stateName)
        {
            Data = Multiverse.DTF_OBJECT_DATA[typeof(T)];
            
            _stateName = stateName;
        }

        internal Position(Vector<double> stateVector)
        {
            Data = Multiverse.DTF_OBJECT_DATA[typeof(T)];
            
            //Acquire the index of the first non-zero value. We are assuming it's one because that is the only use case
            var storage = (SparseVectorStorage<double>) stateVector.Storage;

            _stateName = Data.GetStateName(storage.Indices[0]);
        }

        public override string ToString()
        {
            return _stateName;
        }
    }
}