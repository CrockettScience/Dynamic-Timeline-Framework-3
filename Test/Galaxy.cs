﻿using DTF3.Core;
using DTF3.DTFObjects;

namespace Test
{
    [DTFObject("Galaxy")]
    public class Galaxy : DTFObject
    {
        public Galaxy(Multiverse mVerse) : base(mVerse)
        {
            Register(this);
        }
    }
}