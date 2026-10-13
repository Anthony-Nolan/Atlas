using System;
using Atlas.HlaMetadataDictionary.ExternalInterface;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders
{
    public interface IActiveHlaNomenclatureVersionAccessor
    {

        /// <summary>
        /// Indicates whether or not an active HLA version exists.
        /// </summary>
        /// <returns>
        /// If the active value doesn't exist, or is empty, returns false;
        /// Otherwise true
        /// </returns>
        bool DoesActiveHlaNomenclatureVersionExist();

        /// <returns>
        /// The version of the HLA Nomenclature used to populate the current Transient donor database.
        /// If the transient database has not yet been populated, calling this will result in an Exception.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if the active value is empty.</exception>
        string GetActiveHlaNomenclatureVersion();
    }

    public class ActiveHlaNomenclatureVersionAccessor : IActiveHlaNomenclatureVersionAccessor
    {
        private readonly IActiveDataRefreshRecordAccessor activeDataRefreshRecordAccessor;

        private const string ActiveVersionKey = "activeWmdaVersion";

        public ActiveHlaNomenclatureVersionAccessor(IActiveDataRefreshRecordAccessor activeDataRefreshRecordAccessor)
        {
            this.activeDataRefreshRecordAccessor = activeDataRefreshRecordAccessor;
        }

        public bool DoesActiveHlaNomenclatureVersionExist()
        {
            return IsDefined(GetVersion());
        }

        public string GetActiveHlaNomenclatureVersion()
        {
            var version = GetVersion();
            ThrowIfNull(version, ActiveVersionKey);
            return version;
        }

        /// <remarks>
        /// Read from the same cached record as <see cref="TransientSqlDatabase.ActiveDatabaseProvider"/>, so the version
        /// always belongs to the active database.
        /// </remarks>
        private string GetVersion() => activeDataRefreshRecordAccessor.GetActiveRecord()?.HlaNomenclatureVersion;

        private void ThrowIfNull(string wmdaDatabaseVersion, string key)
        {
            if (!IsDefined(wmdaDatabaseVersion))
            {
                throw new ArgumentNullException(nameof(wmdaDatabaseVersion),
                    $"Attempted to retrieve the {key}, but found <{wmdaDatabaseVersion}>. This is never an appropriate value, under any circumstances, and would definitely cause myriad problems elsewhere.");
            }
        }

        /// <remarks>
        /// Note that this isn't attempting to check whether the version is *VALID*, just whether or not we've got something that purports to be a version.
        /// </remarks>
        private bool IsDefined(string version) => !string.IsNullOrWhiteSpace(version);
    }
}