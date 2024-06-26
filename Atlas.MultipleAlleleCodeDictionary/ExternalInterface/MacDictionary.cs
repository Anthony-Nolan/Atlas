﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface
{
    public interface IMacDictionary
    {
        /// <summary>
        /// Fetch the HLA for a given MAC from the storage account, caching appropriately.
        /// </summary>
        public Task<Mac> GetMac(string macCode);

        /// <remarks>
        /// This does not guarantee that HLA generated will be valid.
        /// For instance, a generic MAC might not be valid for a given first field.
        ///
        /// Even if all alleles produced are valid, they may not be valid at all loci - the MAC dictionary is locus independent.
        ///
        /// Note that in some cases a technically invalid input will be expanded to perfectly valid alleles.
        /// This is the case for specific MACs with the incorrect first field. Technically only one first field is permitted per-specific MAC,
        /// but this dictionary will expand specific MACs ignoring the given first field.
        /// </remarks>
        public Task<IEnumerable<string>> GetHlaFromMac(string firstField, string mac);

        /// <param name="macWithFirstField">
        /// Should be in the format "01:AB". Where 01 is the first field.
        /// </param>
        public Task<IEnumerable<string>> GetHlaFromMac(string macWithFirstField);
    }

    public class MacDictionary : IMacDictionary
    {
        private readonly IMacCacheService macCacheService;

        public MacDictionary(IMacCacheService macCacheService)
        {
            this.macCacheService = macCacheService;
        }

        public async Task<Mac> GetMac(string macCode)
        {
            return await macCacheService.GetMacCode(macCode);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetHlaFromMac(string firstField, string mac)
        {
            return await macCacheService.GetHlaFromMac(firstField, mac);
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetHlaFromMac(string macWithFirstField)
        {
            var parts = macWithFirstField.Split(MolecularTypingNameConstants.FieldDelimiter);
            if (parts.Length != 2)
            {
                throw new ArgumentException($"{macWithFirstField} is not a valid mac.");
            }
            var firstField = parts[0];
            var mac = parts[1];

            return await macCacheService.GetHlaFromMac(mac, firstField);
        }
    }
}