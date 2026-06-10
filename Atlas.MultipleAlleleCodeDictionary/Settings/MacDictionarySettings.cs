using System.ComponentModel.DataAnnotations;

namespace Atlas.MultipleAlleleCodeDictionary.Settings;

/// <summary>
/// Settings needed in all use-cases of the MAC Dictionary component.
/// </summary>
public class MacDictionarySettings
{
    [Required(AllowEmptyStrings = false)]
    public string AzureStorageConnectionString { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string TableName { get; set; }
}