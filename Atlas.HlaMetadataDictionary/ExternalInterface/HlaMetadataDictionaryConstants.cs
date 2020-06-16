namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    public static class HlaMetadataDictionaryConstants
    {
        /// <summary>
        /// There is a single use case when having no nomenclature version is permitted:
        ///     When refreshing the metadata dictionary for the first time.
        /// In this case, a default value is passed instead of null to be clear that this is expected. 
        /// </summary>
        public const string NoActiveVersionValue = "NO-ACTIVE-VERSION";
    }
}