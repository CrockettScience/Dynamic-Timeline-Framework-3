using System;
using DTF3.Core;
using DTF3.DTFObjects;
using DTF3.Internal.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Storage;

namespace DTF3.Internal
{
    internal class StateVector: IStateVector
    {
        public Vector<double> Vector { get; set; }
        public Multiverse.DTFObjectData Data { get; }

        public bool IsWildCard => Vector == null;

        public StateVector(Position fromPosition)
        {
            Data = fromPosition.Data;
            
            var index = Data.MetaData.TransitionIndex(fromPosition.ToString());
            Vector = Vector<double>.Build.Sparse(Data.MetaData.States.Count, i => i == index ? 1 : 0);
        }

        public StateVector(Vector<double> vector, Multiverse.DTFObjectData data)
        {
            Vector = vector;
            Data = data;
        }

        public static StateVector BalancedState(Multiverse.DTFObjectData data)
        {
            var vec = Vector<double>.Build.Sparse(data.MetaData.States.Count, 1.0 / data.MetaData.States.Count);
            
            return new StateVector(vec, data);
        }

        public static StateVector WildCardState(Multiverse.DTFObjectData data)
        {
            return new StateVector(null, data);
        }

        public IStateVector GetTransition(ulong time)
        {
            if (IsWildCard)
            {
                //Wildcard state, return balanced state as transition
                return BalancedState(Data);
            }
            
            var probabilityMatrix = Power(Data.MetaData.TransitionMatrix, time);
            
            //ProbabilityMatrix is a dense matrix for faster operations, but the output vector needs to be sparse
            var denseOutput = Vector * probabilityMatrix;
            
            var vec = Vector<double>.Build.Sparse(denseOutput.Count, i => denseOutput[i]);
            return new StateVector(vec, Data);
        }

        public bool IsTransitionableTo(Position other)
        {
            if (IsWildCard)
                return true;
            
            var otherState = new StateVector(other);
            
            var probabilityMatrix = Power(Data.MetaData.TransitionMatrix, 1);

            
            //ProbabilityMatrix is a dense matrix for faster operations, but the output vector needs to be sparse
            var denseOutput = Vector * probabilityMatrix;
            var transitionVector = Vector<double>.Build.Sparse(denseOutput.Count, i => denseOutput[i]);

            var possibleIndex = ((SparseVectorStorage<double>) otherState.Vector.Storage).Indices[0];
            foreach (var index in ((SparseVectorStorage<double>) transitionVector.Storage).Indices)
            {
                if (index == possibleIndex)
                    return true;
            }

            return false;
        }

        public void Collapse(Random rand)
        {
            var val = rand.NextDouble();

            for (var i = 0; i < Vector.Count; i++)
            {
                var prob = Vector[i];
                val -= prob;

                if (val < 0)
                {
                    Vector = Vector<double>.Build.Sparse(Vector.Count);
                    Vector[i] = 1;
                    return;
                }
                    
            }
        }

        public IStateVector SafeMask(UniverseTree.Branch branch, ulong date, DTFObject obj, PossibilityMask mask)
        {
            try
            {
                return this * mask;
            }
            catch (DivideByZeroException)
            {
                //This happens if a state's length is "stretched" too far beyond the allowed precision.
                return branch.Guess(date, obj);
            }
        }

        public static implicit operator Position(StateVector sVector)
        {
            var storage = (SparseVectorStorage<double>) sVector.Vector.Storage;
            return new Position(sVector.Data.MetaData.StateName(storage.Indices[0]), sVector.Data);
        }
        
        public static implicit operator StateVector(Position pos)
        {
            return new StateVector(pos);
        }
        
        private static Matrix<double> Power(Matrix<double> m, ulong exponent)
        {
            if (m.RowCount != m.ColumnCount)
                throw new ArgumentException("Matrix must be square.");

            switch (exponent)
            {
                case 0:
                    return Matrix<double>.Build.DiagonalIdentity(m.RowCount, m.ColumnCount);
                case 1:
                    return m;
                case 2:
                    return m.Multiply(m);
                default:
                    return UlongPower(exponent, m.Clone(), null, null);
            }
        }

        private static Matrix<double> UlongPower(ulong exponent, Matrix<double> x, Matrix<double> y, Matrix<double> work)
        {
            //Standard recursive divide and conquer algorithm
            switch (exponent)
            {

                case 1:
                    if (y == null)
                        return x;

                    if (work == null)
                        work = y.Multiply(x);

                    else
                        y.Multiply(x, work);

                    return work;

                case 2:
                    if (work == null)
                        work = x.Multiply(x);

                    else
                        x.Multiply(x, work);

                    if (y == null)
                        return work;

                    y.Multiply(work, x);

                    return x;

                default:
                    if (exponent % 2UL == 0)
                    {
                        if (work == null)
                            work = x.Multiply(x);

                        else
                            x.Multiply(x, work);

                        return UlongPower(exponent / 2, work, y, x);
                    }

                    if (y == null)
                    {
                        if (work == null)
                            work = x.Multiply(x);

                        else
                            x.Multiply(x, work);

                        return UlongPower((exponent - 1) / 2, work, x, null);
                    }

                    if (work == null)
                        work = y.Multiply(x);

                    else
                        y.Multiply(x, work);
                    x.Multiply(x, y);

                    return UlongPower((exponent - 1) / 2, y, work, x);
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StateVector other))
                return false;

            return IsWildCard ? other.IsWildCard : Vector.Equals(other.Vector);
        }

        public override string ToString()
        {
            var str = "";

            for (var i = 0; i < Data.MetaData.States.Count; i++)
            {
                str += $"{Data.MetaData.States[i].StateName}: {(Vector == null ? "any" : Vector[i].ToString())}\n";
            }

            return str.TrimEnd();
        }
    }
}