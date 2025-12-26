using System;
using System.Collections.Generic;
using System.Text;

namespace WatchDog.src.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
    public class IgnoreWatchAttribute: Attribute
    {

    }
}
