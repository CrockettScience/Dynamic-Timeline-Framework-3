using System;
using DTF3.DTFObjects;
using DTF3.Exception;
using DTF3.Internal;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Storage;

namespace DTF3.Core
{
    public class Continuity
    {
        private readonly DTFObject _dtfObject;
        private readonly Universe _universe;

        internal Continuity(DTFObject dtfObject, Universe universe)
        {
            //Todo - Universe needs to be the "real" universe that affects this object. It will either be it or one if it's parents
            _dtfObject = dtfObject;
            _universe = universe;
        }

        /// <summary>
        /// Gets the position of the object at the specified date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public Position Measure(ulong date)
        {
            if (date == 0)
                return StateVector.BalancedState(_dtfObject.Data);
            
            //Get forecast vector
            var branch = _universe.UniverseTree.Branches[_universe.Diff];
            var forecast = branch.Forecast(date, _dtfObject);

            forecast.Collapse(_universe.UniverseTree.Random);
            branch.InsertState(date, _dtfObject, forecast);
            
            //Return position
            return (StateVector) forecast;
        }
        
        /// <summary>
        /// Asserts the object is a given position at a given date.
        /// </summary>
        /// <param name="date">The date to assert the position at. Cannot be 0</param>
        /// <param name="pos">The position to assert</param>
        /// <param name="diff">Output variable. If the position is coherent with past positions on this timeline but
        /// is NOT coherent with future positions on this timeline, a diff will be created that can be used to create
        /// a new universe with the desired position</param>
        /// <returns>True if the position is successfully asserted on this timeline</returns>
        public bool Assert(ulong date, Position pos, out Diff diff)
        {
            if (date == 0)
                throw new DTFException("Cannot assert any position onto the timeline at date 0.");
            
            //Get StateVector
            var vec = (StateVector) pos;
            var branch = _universe.UniverseTree.Branches[_universe.Diff];

            if (branch.GetStateAtOrBefore(date, _dtfObject).IsTransitionableTo(vec))
            {
                //Determine if given state is possible
                var stateIndex = ((SparseVectorStorage<double>) vec.Vector.Storage).Indices[0];
                var futureMask = branch.FutureMask(date, _dtfObject);
                
                //The position is coherent with past positions
                if (futureMask[stateIndex])
                {
                    //The position is coherent with future positions
                    branch.InsertState(date, _dtfObject, vec);
                    
                    //Todo - When we collapse the state for this object, we must also collapse the parent to a state to that is coherent with it's children
                    
                    diff = null;
                    return true;
                }
                
                //The position is NOT coherent with future positions
                diff = new Diff(_universe, date, _dtfObject, pos);
                return false;
            }

            //The position is not coherent with past or future positions
            diff = null;
            return false;
        }
    }
}