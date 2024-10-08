﻿using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors.AlleleGroupExtractors
{
    internal class PGroupExtractor : AlleleGroupExtractorBase<HlaNomP>
    {
        private const string FileName = "hla_nom_p.txt";

        public PGroupExtractor() : base(FileName)
        {
        }
    }
}
