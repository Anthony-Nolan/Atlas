using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using FluentAssertions;
using NUnit.Framework;
using TypeExtensions = Atlas.Common.Utils.Extensions.TypeExtensions;

namespace Atlas.Common.Test.Core.Reflection
{
    [TestFixture]
    public class TypeExtensionTests
    {
        [Test]
        public void GetNeatCSharpName_GetNeatCSharpName_GivenPrimitiveType_CalculatesName()
        {
            typeof(Int32).GetNeatCSharpName().Should().Be("Int32");
            typeof(Boolean).GetNeatCSharpName().Should().Be("Boolean");
        }

        [Test]
        public void GetNeatCSharpName_GivenFriendlyPrimitiveTypes_CalculatesFormalNames()
        {
            typeof(int).GetNeatCSharpName().Should().Be("Int32");
            typeof(bool).GetNeatCSharpName().Should().Be("Boolean");
            typeof(double).GetNeatCSharpName().Should().Be("Double");
            typeof(decimal).GetNeatCSharpName().Should().Be("Decimal");
        }

        [Test]
        public void GetNeatCSharpName_GivenSimpleFrameworkTypes_CalculatesName()
        {
            typeof(DateTime).GetNeatCSharpName().Should().Be("DateTime");
            typeof(StringBuilder).GetNeatCSharpName().Should().Be("StringBuilder");
        }

        [Test]
        public void GetNeatCSharpName_GivenFrameworkInterfaces_CalculatesName()
        {
            typeof(IDisposable).GetNeatCSharpName().Should().Be("IDisposable");
            typeof(IEnumerable).GetNeatCSharpName().Should().Be("IEnumerable");
        }

        [Test]
        public void GetNeatCSharpName_GivenSimpleCodebaseTypes_CalculatesName()
        {
            typeof(TypeExtensions).GetNeatCSharpName().Should().Be("TypeExtensions");
            typeof(HlaTypingCategory).GetNeatCSharpName().Should().Be("HlaTypingCategory");
        }

        [Test]
        public void GetNeatCSharpName_GivenGenericClass_WithOnePrimitiveArg_CalculatesName()
        {
            typeof(List<int>).GetNeatCSharpName().Should().Be("List<Int32>");
            typeof(IEquatable<decimal>).GetNeatCSharpName().Should().Be("IEquatable<Decimal>");
        }

        [Test]
        public void GetNeatCSharpName_GivenGenericClass_WithOneClassArg_CalculatesName()
        {
            typeof(IEnumerable<DateTime>).GetNeatCSharpName().Should().Be("IEnumerable<DateTime>");
            typeof(Comparer<string>).GetNeatCSharpName().Should().Be("Comparer<String>");
        }

        [Test]
        public void GetNeatCSharpName_GivenGenericClass_WithMultipleArg_CalculatesName()
        {
            typeof(Tuple<int, TypeExtensionTests, DateTime, String>).GetNeatCSharpName().Should().Be("Tuple<Int32, TypeExtensionTests, DateTime, String>");
        }

        [Test]
        public void GetNeatCSharpName_GivenNestedGenericClass_CalculatesName()
        {
            typeof(List<Tuple<int, List<bool>, Dictionary<DateTime, IComparable<Tuple<decimal, double>>>>>).GetNeatCSharpName().Should().Be("List<Tuple<Int32, List<Boolean>, Dictionary<DateTime, IComparable<Tuple<Decimal, Double>>>>>");
        }

        [Test]
        public void GetNeatCSharpName_GivenNullableType_DeclaredAsGeneric_CalculatesName()
        {
            typeof(Nullable<int>).GetNeatCSharpName().Should().Be("Nullable<Int32>");
        }

        [Test]
        public void GetNeatCSharpName_GivenNullableType_DeclaredAsFriendly_CalculatesName()
        {
            typeof(bool?).GetNeatCSharpName().Should().Be("Nullable<Boolean>");
        }

        [Test]
        public void GetNeatCSharpName_GivenSubclassType_CalculatesSomeRepresentation()
        {
            //Don't think we really care about this much of an edge case. This test is just documenting current behaviour.
            //Feel free to change the behaviour if you think a different behaviour is necessary.
            typeof(Dictionary<int, string>.KeyCollection).GetNeatCSharpName().Should().Be("KeyCollection<Int32, String>");
        }

        [Test]
        public void GetNeatCSharpName_GivenTypeFromGenericMethod_CalculatesName()
        {
            Test<int>("Int32");
            Test<Int32>("Int32");
            Test<StringBuilder>("StringBuilder");
            Test<ISerializable>("ISerializable");
            Test<Locus>("Locus");
            Test<List<Tuple<double, TestAttribute>>>("List<Tuple<Double, TestAttribute>>");
        }

        public void Test<T>(string expectedAnswer)
        {
            typeof(T).GetNeatCSharpName().Should().Be(expectedAnswer);
        }

        [Test]
        public void GetNeatCSharpName_GivenType_CalculatesName_FromGenericInvocationForm()
        {
            TypeExtensions.GetNeatCSharpName<int>().Should().Be("Int32");
            TypeExtensions.GetNeatCSharpName<Int32>().Should().Be("Int32");
            TypeExtensions.GetNeatCSharpName<StringBuilder>().Should().Be("StringBuilder");
            TypeExtensions.GetNeatCSharpName<ISerializable>().Should().Be("ISerializable");
            TypeExtensions.GetNeatCSharpName<Locus>().Should().Be("Locus");
            TypeExtensions.GetNeatCSharpName<List<Tuple<double, TestAttribute>>>().Should().Be("List<Tuple<Double, TestAttribute>>");
        }
    }
}
