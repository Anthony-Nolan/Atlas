using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// Derives <c>SubjectGenotypeSetValue.HlaTypingKey</c>: the fixed-length stand-in for a donor's HLA typing that lets
/// two donors with the same typing share one stored payload.
///
/// <para>
/// <b>It covers only the loci in the combination.</b> Every stage of imputation gates on <c>AllowedLoci</c> -
/// <c>CompressedPhenotypeConverter.ConvertPhenotype</c>, <c>CompressedPhenotypeExpander.GetHaplotypesForAllowedLoci</c>
/// (which nulls excluded loci outright) and <c>GenotypeConverter.GetNullAlleleInfo</c> - so the result depends on
/// nothing else. Two donors identical at {A, B, DRB1} but different at C therefore share one <c>ABDrb1</c> row.
/// </para>
///
/// <para>
/// <b>Derivable from the raw typing with no HLA Metadata Dictionary round trip.</b> That is what makes the
/// orchestrator's skip-existing step worth anything: keys are free, imputation is the expensive part.
/// </para>
/// </summary>
public static class SubjectGenotypeSetKeyGenerator
{
    /// <summary>
    /// Separates positions in the canonical string. U+001F, the ASCII unit separator: an HLA name can hold <c>*</c>,
    /// <c>:</c>, letters and digits, but never a control character - so no typing can forge a boundary and two
    /// different typings cannot canonicalise to one string.
    ///
    /// <para>
    /// Written as an escape rather than as the literal byte. The two compile to the same <c>char</c>, but a raw
    /// control character is invisible in a diff and is the kind of thing a formatter or a copy-paste can strip.
    /// </para>
    /// </summary>
    private const char PositionSeparator = '\u001F';

    /// <summary>
    /// The order positions are joined in. A frozen contract, like the payload's own slot table: reordering it
    /// changes every key, and every stored row would then be missed and recomputed.
    /// </summary>
    private static readonly LocusPosition[] Positions = [LocusPosition.One, LocusPosition.Two];

    /// <summary>
    /// Loci in a fixed order, so the key does not depend on how the caller's set enumerates. Dpb1 is absent by
    /// construction - no <see cref="AllowedLociKey"/> includes it.
    /// </summary>
    private static readonly Locus[] LocusOrder = [Locus.A, Locus.B, Locus.C, Locus.Drb1, Locus.Dqb1];

    /// <summary>
    /// SHA-256 of the canonical string, as <b>lower-case hex</b>.
    ///
    /// <para>
    /// <b>Hex, not Base64 - a correctness requirement, not a style one.</b> The column is <c>nvarchar(64)</c> under
    /// a case-insensitive collation, so two Base64 digests differing only in letter case would be one row to SQL
    /// Server and two distinct strings to C#: the get-or-create would insert, the unique index would reject it, and
    /// the join-back would return the wrong id. Hex has no case-distinct pairs, so the two agree.
    /// </para>
    /// </summary>
    public static string GenerateHlaTypingKey(PhenotypeInfo<string> hlaTyping, AllowedLociKey allowedLociKey)
    {
        ArgumentNullException.ThrowIfNull(hlaTyping);

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(Canonicalise(hlaTyping, allowedLociKey.ToLoci())));

        return Convert.ToHexStringLower(digest);
    }

    /// <summary>
    /// The string that is hashed. Exposed for the tests that pin it: a key is only as trustworthy as the string it
    /// came from, and a silent change to this is a silent change to every key.
    /// </summary>
    /// <remarks>
    /// Null and empty give the same string, deliberately: <c>StringBuilder.Append</c> adds nothing for either. The
    /// matching algorithm reads both as an untyped position, so two donors that differ only this way must share one row.
    /// Imputation does NOT read them the same - it sends an empty name to the HLA Metadata Dictionary, which throws. The
    /// shared row is correct because <see cref="SubjectGenotypeSetPrecomputeService"/> changes each empty name to null
    /// before it imputes.
    /// </remarks>
    internal static string Canonicalise(PhenotypeInfo<string> hlaTyping, IReadOnlySet<Locus> allowedLoci)
    {
        var builder = new StringBuilder(128);

        foreach (var locus in LocusOrder)
        {
            if (!allowedLoci.Contains(locus))
            {
                continue;
            }

            foreach (var position in Positions)
            {
                builder.Append(hlaTyping.GetPosition(locus, position)).Append(PositionSeparator);
            }
        }

        return builder.ToString();
    }
}
