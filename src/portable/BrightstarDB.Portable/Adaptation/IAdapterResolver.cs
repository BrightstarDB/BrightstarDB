using System;

namespace BrightstarDB.Portable.Adaptation
{
    internal interface IAdapterResolver
    {
        object Resolve(Type type);
    }
}