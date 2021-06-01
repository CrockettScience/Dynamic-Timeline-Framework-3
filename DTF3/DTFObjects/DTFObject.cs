using System;
using System.Collections.Generic;
using DTF3.Core;
using DTF3.Exception;
using DTF3.Internal;

namespace DTF3.DTFObjects
{
    public abstract class DTFObject
    {
        public IEnumerable<DTFObject> GetLateralObjects => _lateralMaps.Values;
        public IEnumerable<string> GetLateralKeys => _lateralKeys;
        public int LateralObjectCount => _lateralMaps.Count;
        public bool HasLateralObjects => HasParent ? LateralObjectCount > 1 : LateralObjectCount > 0;
        public DTFObject Parent => HasParent ? _lateralMaps[Data.MetaData.ParentKey] : null;
        public bool HasParent => Data.MetaData.ParentKey != null;
        
        
        internal Multiverse.DTFObjectData Data { get; }
        internal ObjectTree.Node ObjectNode { get; set; }

        
        private Dictionary<string, DTFObject> _lateralMaps;
        private HashSet<string> _lateralKeys;
        private Multiverse _multiverse;
        

        public DTFObject GetLateralObject(string key) => _lateralMaps[key];
        public bool ContainsLateral(string key) => _lateralMaps.ContainsKey(key);

        protected DTFObject(Multiverse multiverse)
        {
            _multiverse = multiverse;
            Data = multiverse.GetObjectData(GetType());
            _lateralMaps = new Dictionary<string, DTFObject>();
            _lateralKeys = new HashSet<string>();
        }

        protected void AddLateral(string key, DTFObject dtfObject)
        {
            //Cannot reassign lateral object if the object is already set
            if(_lateralMaps.ContainsKey(key))
                throw new DTFException("Lateral object already set");
            
            //Lateral objects are a two way assignment
            _lateralMaps[key] = dtfObject;
            _lateralKeys.Add(key);
            dtfObject.AddReverseLateral(key, dtfObject);
        }

        protected void SetParent(DTFObject parent)
        {
            //Cannot reassign lateral object if the object is already set
            if(_lateralMaps.ContainsKey(Data.MetaData.ParentKey))
                throw new DTFException("Parent object already set");
            
            _lateralMaps[Data.MetaData.ParentKey] = parent;
        }
        
        protected static void Register<T>(T obj) where T : DTFObject
        {
            var node = obj._multiverse.RegisterObject(obj);
            obj.ObjectNode = node;
        }

        private void AddReverseLateral(string key, DTFObject dtfObject)
        {
            var reverseKey = key + "_REVERSE_";
            
            var i = 0;
            while (_lateralMaps.ContainsKey(reverseKey + i))
                i++;
            
            _lateralMaps[reverseKey + i] = dtfObject;
        }
    }
}