using System;

namespace DTF3.DTFObjects
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DTFObjectAttribute : Attribute
    {
        public readonly string TypeName;

        public DTFObjectAttribute(string typeName)
        {
            TypeName = typeName;
        }
    }
}