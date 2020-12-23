using System;
using DTF3.Core;
using DTF3.DTFObjects;
using DTF3.Internal.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Storage;

namespace DTF3.Internal
{
    internal class StateVector<T> : IStateVector<T> where T : DTFObject
    {
        public Vector<double> Vector { get; private set; }
        private readonly Multiverse.DTFObjectData _data;

        public StateVector(Position<T> fromPosition)
        {
            _data = fromPosition.Data;
            
            var index = _data.GetTransitionIndex(fromPosition.ToString());
            Vector = Vector<double>.Build.Sparse(_data.States.Count, i => i == index ? 1 : 0);
        }

        private StateVector(Vector<double> vector, Multiverse.DTFObjectData data)
        {
            Vector = vector;
            _data = data;
        }

        public StateVector<T> GetTransition(ulong time)
        {
            var probabilityMatrix = Power(_data.TransitionMatrix, time);
            
            //ProbabilityMatrix is a dense matrix for faster operations, but the output vector needs to be sparse
            var denseOutput = Vector * probabilityMatrix;
            
            var vec = Vector<double>.Build.Sparse(denseOutput.Count, i => denseOutput[i]);
            return new StateVector<T>(vec, _data);
        }

        public void Mask(PossibilityMask<T> mask)
        {
            Vector *= mask.Matrix;
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

        public static implicit operator Position<T>(StateVector<T> sVector)
        {
            var storage = (SparseVectorStorage<double>) sVector.Vector.Storage;
            return new Position<T>(sVector._data.GetStateName(storage.Indices[0]));
        }
        
        public static implicit operator StateVector<T>(Position<T> pos)
        {
            return new StateVector<T>(pos);
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
            if (!(obj is StateVector<T> other))
                return false;

            return Vector.Equals(other.Vector);
        }

        public override int GetHashCode()
        {
            return Vector.GetHashCode();
        }
    }
}