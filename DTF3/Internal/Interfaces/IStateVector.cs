using System;
using DTF3.Core;
using DTF3.DTFObjects;
using MathNet.Numerics.LinearAlgebra;

namespace DTF3.Internal.Interfaces
{
    internal interface IStateVector
    {
        Multiverse.DTFObjectData Data { get; }
        Vector<double> Vector { get; set; }
        IStateVector GetTransition(ulong time);
        bool IsTransitionableTo(Position other);
        void Collapse(Random rand);

        IStateVector SafeMask(UniverseTree.Branch branch, ulong date, DTFObject obj, PossibilityMask mask);
    }
}