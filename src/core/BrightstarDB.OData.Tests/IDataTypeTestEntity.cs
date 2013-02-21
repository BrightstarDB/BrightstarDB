using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData.Tests
{
    [Entity]
    public interface IDataTypeTestEntity
    {
        [Identifier("http://example.org/tests/")]
        string Id { get; }

        string SomeString { get; set; }

        DateTime SomeDateTime { get; set; }

        DateTime? SomeNullableDateTime { get; set; }

        bool SomeBool { get; set; }

        bool? NullableBool { get; set; }

        Byte SomeByte { get; set; }
        byte AnotherByte { get; set; }
        Byte? NullableByte { get; set; }
        byte? AnotherNullableByte { get; set; }

        //Char SomeChar { get; set; }
        //char AnotherChar { get; set; }
        //Char? NullableChar { get; set; }
        //char? AnotherNullableChar { get; set; }

        decimal SomeDecimal { get; set; }

        double SomeDouble { get; set; }

        float SomeFloat { get; set; }

        int SomeInt { get; set; }

        int? SomeNullableInt { get; set; }

        long SomeLong { get; set; }

        sbyte SomeSByte { get; set; }
        SByte AnotherSByte { get; set; }

        Int16 SomeShort { get; set; }
        short AnotherShort { get; set; }

        UInt32 SomeUInt { get; set; }
        uint AnotherUInt { get; set; }

        UInt64 SomeULong { get; set; }
        ulong AnotherULong { get; set; }

        UInt16 SomeUShort { get; set; }
        ushort AnotherUShort { get; set; }

        Byte[] SomeByteArray { get; set; }

        TestEnumeration SomeEnumeration { get; set; }

        ICollection<string> CollectionOfStrings { get; set; }
        ICollection<DateTime> CollectionOfDateTimes { get; set; }
        ICollection<bool> CollectionOfBools { get; set; }
        ICollection<Decimal> CollectionOfDecimals { get; set; }
        ICollection<Double> CollectionOfDoubles { get; set; }
        ICollection<float> CollectionOfFloats { get; set; }
        ICollection<int> CollectionOfInts { get; set; }
        ICollection<long> CollectionOfLong { get; set; }
    }

    public enum TestEnumeration
    {
        First,
        Second,
        Third
    }
}
