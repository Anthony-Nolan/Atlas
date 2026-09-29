using System;

namespace Atlas.HlaMetadataDictionary.InternalModels
{
    /// <param name="Stamp">See <see cref="HlaMetadataRecreationRow.Stamp"/>.</param>
    /// <param name="RecreatedAtUtc">By the clock of the machine that did the recreating.</param>
    internal record HlaMetadataRecreationStamp(string Stamp, DateTimeOffset RecreatedAtUtc);
}
