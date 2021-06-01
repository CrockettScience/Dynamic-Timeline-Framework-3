using System.Collections.Generic;
using DTF3.DTFObjects;
using DTF3.Internal;
using DTF3.Internal.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Storage;

namespace DTF3.Core
{
    public class Position: IPosition
    {
        internal readonly Multiverse.DTFObjectData Data;
        private readonly string _stateName;

        public Position(string stateName, DTFObject obj)
        {
            Data = obj.Data;
            _stateName = stateName;
        }
        
        internal Position(string stateName, Multiverse.DTFObjectData data)
        {
            Data = data;
            _stateName = stateName;
        }

        public override string ToString()
        {
            return _stateName;
        }
    }
}