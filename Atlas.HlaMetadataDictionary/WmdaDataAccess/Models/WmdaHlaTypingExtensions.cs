﻿using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.HlaTypingInfo;

namespace Atlas.HlaMetadataDictionary.WmdaDataAccess.Models
{
    internal static class WmdaHlaTypingExtensions
    {
        private static class Drb345Serologies
        {
            public const string Locus = "DR";
            public static readonly string[] Typings = { "51", "52", "53" };
        }

        public static bool IsDrb345SerologyTyping(this IWmdaHlaTyping typing)
        {
            return typing.TypingLocus.Equals(Drb345Serologies.Locus) && Drb345Serologies.Typings.Contains(typing.Name);
        }

        public static bool IsHlaMetadataDictionaryLocusTyping(this IWmdaHlaTyping typing)
        {
            return typing.TypingMethod == TypingMethod.Molecular ?
                HlaMetadataDictionaryLoci.IsMolecularLocus(typing.TypingLocus) :
                    HlaMetadataDictionaryLoci.IsSerologyLocus(typing.TypingLocus) && !typing.IsDrb345SerologyTyping();
        }

        public static bool TypingEquals(this IWmdaHlaTyping typing, IWmdaHlaTyping other)
        {
            return typing.TypingLocus.Equals(other.TypingLocus) && typing.Name.Equals(other.Name);
        }

        public static bool LocusEquals(this IWmdaHlaTyping typing, IWmdaHlaTyping other)
        {
            return typing.TypingLocus.Equals(other.TypingLocus);
        }
    }
}
