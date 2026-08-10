namespace Atlas.MultipleAlleleCodeDictionary.Services;

/// <summary>
/// Compact in-memory representation of a MAC, for storage in <see cref="IMacStore"/>.
/// The MAC's code is the store's dictionary key, so it is deliberately not repeated here.
/// A struct, so that entries live inline in the dictionary's value array, with no per-entry object header.
/// The public <see cref="ExternalInterface.Models.Mac"/> is reconstructed on the way out of the store.
/// </summary>
internal readonly record struct MacValue(string Hla, bool IsGeneric);
