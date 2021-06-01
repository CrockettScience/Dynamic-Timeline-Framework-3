using System;
using System.Runtime.InteropServices;
using DTF3.Core;
using DTF3.DTFObjects;
using DTF3.Internal.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Storage;

namespace DTF3.Internal
{
    /// <summary>
    /// Represents a virtual boolean mask that can be multiplied
    /// by a StateVector to eliminate "possibilities" and redistribute it's probability across the
    /// states that aren't thrown out
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class PossibilityMask
    {
        public Multiverse.DTFObjectData Data { get; }

        public Vector<double> _possibilities;

        public bool this[int i]
        {
            get => _possibilities[i] != 0;
            
            set => _possibilities[i] = value ? 1 : 0;
        }

        public int Length => _possibilities.Count;
        
        public PossibilityMask(IStateVector fromVector)
        {
            Data = fromVector.Data;

            _possibilities = Vector<double>.Build.Sparse(fromVector.Vector.Count);

            var storage = (SparseVectorStorage<double>) fromVector.Vector.Storage;

            foreach (var nonZeroIndex in storage.Indices)
            {
                _possibilities[nonZeroIndex] = 1.0;
            }
        }

        public static PossibilityMask GetOpenMask(Multiverse.DTFObjectData data)
        {
            return new PossibilityMask(StateVector.BalancedState(data));
        }

        public void Or(PossibilityMask other)
        {
            for (var i = 0; i < Length; i++)
                this[i] = this[i] | other[i];
        }

        public void And(PossibilityMask other)
        {
            for (var i = 0; i < Length; i++)
                this[i] = this[i] & other[i];
        }

        public void ProjectForward()
        {
            var stateVector = StateVector.BalancedState(Data) * this;

            foreach (var state in Data.MetaData.States)
            {
                var other = new StateVector(new Position(state.StateName, Data));
                if (stateVector.IsTransitionableTo(other))
                {
                    Or(new PossibilityMask(other));
                }
            }
        }

        public void ProjectBackward()
        {
            var stateVector = StateVector.BalancedState(Data) * this;

            foreach (var state in Data.MetaData.States)
            {
                var other = new StateVector(new Position(state.StateName, Data));
                if (other.IsTransitionableTo(stateVector))
                {
                    Or(new PossibilityMask(other));
                }
            }
        }

        public static StateVector operator *(StateVector vector, PossibilityMask mask)
        {
            var vectorIndices = ((SparseVectorStorage<double>) vector.Vector.Storage).Indices;
            var length = mask.Length;

            //Construct a redistribution vector
            var rVector = Vector<double>.Build.Sparse(length);
            
            //Calculate the redistribution factor
            var rDenominator = vectorIndices.Length;

            foreach (var nonZeroIndex in vectorIndices)
            {
                if (mask._possibilities[nonZeroIndex] < double.Epsilon)
                {
                    rDenominator--;
                }

                else
                {
                    rVector[nonZeroIndex] = 1.0;
                }
            }

            if (rDenominator == 0)
            {
                throw new DivideByZeroException();
            }
            
            var rFactor = 1.0 / rDenominator;

            rVector *= rFactor;

            //Construct a redistribution matrix
            var matrix = Matrix<double>.Build.Sparse(length, length);
            
            for(var i = 0; i < length; i++)
            {
                matrix.SetRow(i, rVector[i] > double.Epsilon ? Vector<double>.Build.Sparse(length, j => i == j ? 1 : 0) : rVector);
            }

            //Multiply
            return new StateVector(vector.Vector * matrix, mask.Data);
            
        }

        public override string ToString()
        {
            var str = "";
            for (var i = 0; i < Length; i++)
            {
                str += $"{Data.MetaData.States[i].StateName}: {this[i]}\n";
            }

            return str.TrimEnd();
        }
    }
}