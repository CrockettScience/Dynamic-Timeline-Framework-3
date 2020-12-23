using DTF3.DTFObjects;
using DTF3.Internal.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Storage;

namespace DTF3.Internal
{
    /// <summary>
    /// Represents a virtual boolean mask that manipulates an internal matrix that can be multiplied
    /// by a stochastic StateVector to eliminate "possibilities" and redistribute it's probability across the
    /// states that aren't thrown out
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class PossibilityMask<T> : IPossibilityMask<T> where T : DTFObject
    {
        public Matrix<double> Matrix { get; }

        private Vector<double> _redistributionVector;

        public bool this[int i]
        {
            get => Matrix[i, i] != 0.0;

            set
            {
                if (value && _redistributionVector[i] == 0)
                {
                    var nonZeroIndices = ((SparseVectorStorage<double>) _redistributionVector.Storage).Indices;
                    var redistributionFactor = 1 / (nonZeroIndices.Length + 1);

                    _redistributionVector[i] = redistributionFactor;
                        
                    foreach (var j in nonZeroIndices)
                    {
                        _redistributionVector[j] = redistributionFactor;
                    }
                    
                    Reallocate();
                }
                
                else if (!value && _redistributionVector[i] != 0)
                {
                    _redistributionVector[i] = 0;

                    var nonZeroIndices = ((SparseVectorStorage<double>) _redistributionVector.Storage).Indices;

                    if (nonZeroIndices.Length == 0)
                    {
                        //Zero out the whole matrix
                        Matrix.Clear();
                        return;
                    }

                    var redistributionFactor = 1 / nonZeroIndices.Length;
                    
                    foreach (var j in nonZeroIndices)
                    {
                        _redistributionVector[j] = redistributionFactor;
                    }
                    
                    Reallocate();
                }
            }
        }

        public int Length => Matrix.ColumnCount;
        
        public PossibilityMask(StateVector<T> fromVector)
        {
            var vector = fromVector.Vector;
            var length = vector.Count;
            var nonZeroIndices = ((SparseVectorStorage<double>) vector.Storage).Indices;
            var redistributionFactor = 1 / nonZeroIndices.Length;

            _redistributionVector = Vector<double>.Build.Sparse(nonZeroIndices.Length);
            foreach (var j in nonZeroIndices)
            {
                _redistributionVector[j] = redistributionFactor;
            }

            Matrix = Matrix<double>.Build.Sparse(length, length);
            
        }

        public void And(PossibilityMask<T> other)
        {
            for (var i = 0; i < Length; i++)
                this[i] = this[i] & other[i];
        }

        private void Reallocate()
        {
            for (var i = 0; i < Length; i++)
            {
                var val = _redistributionVector[i];
                
                //Note: the closure passed to the build method appears to run in parallel. 
                //For this reason, allocate a copy of i so it is not disrupted by the loop incrementing it.
                var iStatic = i;

                Matrix.SetRow(iStatic,
                    val != 0 ? Vector<double>.Build.Sparse(Length, j => iStatic == j ? 1 : 0) :
                        _redistributionVector);
            }
        }
    }
}