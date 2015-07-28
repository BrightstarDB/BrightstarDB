using System;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    public class LowercaseKeyConverter : DefaultKeyConverter
    {
        public override string GenerateKey(object[] keyValues, string keySeparator, Type forType)
        {
            return base.GenerateKey(keyValues, keySeparator, forType).ToLowerInvariant();
        }
    }
}
