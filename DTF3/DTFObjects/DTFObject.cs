using System.Collections.Generic;
using DTF3.Core;
using DTF3.Exception;
using DTF3.Internal;

namespace DTF3.DTFObjects
{
    public abstract class DTFObject
    {
        private Multiverse.DTFObjectData _data;
        
        private Dictionary<string, DTFObject> _lateralMaps;

        internal ObjectTree.Node ObjectNode { get; set; }

        public DTFObject Parent => HasParent ? _lateralMaps[_data.ParentKey] : null;

        protected void AddLateral(string key, DTFObject dtfObject)
        {
            //Cannot reassign lateral object if the object is already set
            if(_lateralMaps.ContainsKey(_data.ParentKey))
                throw new DTFException("Lateral object already set");
            
            //Lateral objects are a two way assignment
            _lateralMaps[key] = dtfObject;
            dtfObject.AddReverseLateral(key, dtfObject);
        }

        protected void SetParent(DTFObject parent)
        {
            //Cannot reassign lateral object if the object is already set
            if(_lateralMaps.ContainsKey(_data.ParentKey))
                throw new DTFException("Parent object already set");
            
            _lateralMaps[_data.ParentKey] = parent;
        }
        
        protected void Register(Multiverse multiverse)
        {
            var (dtfObjectData, node) = multiverse.RegisterObject(this);
            _data = dtfObjectData;
            ObjectNode = node;
            
            _lateralMaps = new Dictionary<string, DTFObject>();
        }

        private void AddReverseLateral(string key, DTFObject dtfObject)
        {
            var reverseKey = key + "_REVERSE_";
            
            var i = 0;
            while (_lateralMaps.ContainsKey(reverseKey + i))
                i++;
            
            _lateralMaps[reverseKey + i] = dtfObject;
        }

        public DTFObject GetLateralObject(string key) => _lateralMaps[key];

        public bool ContainsLateral(string key) => _lateralMaps.ContainsKey(key);
        
        public bool HasParent => _lateralMaps.ContainsKey(_data.ParentKey);

        public bool HasLateralObjects => HasParent ? LateralObjectCount > 1 : LateralObjectCount > 0;

        public IEnumerable<DTFObject> GetLateralObjects => _lateralMaps.Values;

        public int LateralObjectCount => _lateralMaps.Count;
    }
}