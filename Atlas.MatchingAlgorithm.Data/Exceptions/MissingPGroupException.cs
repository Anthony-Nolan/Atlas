using System;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.MatchingAlgorithm.Data.Exceptions;

/// <summary>
/// A donor's HLA has a P-group that is not in the <c>PGroupNames</c> table. Data Refresh inserts every P-group of the HLA
/// nomenclature version before HLA processing starts, so during a refresh this means that the upfront insert is wrong, for
/// example that it used another nomenclature version than the HLA expansion. The P-group is not inserted silently.
/// </summary>
public class MissingPGroupException : Exception
{
    public MissingPGroupException(string pGroup, string hlaName, Locus locus)
        : base($"P-group '{pGroup}' of HLA '{hlaName}' at locus {locus} is not in the PGroupNames table. " +
               "Data Refresh inserts every P-group of the HLA nomenclature version before HLA processing starts, " +
               "so the upfront P-group insert is wrong, or used another nomenclature version than the HLA expansion.")
    {
    }
}
