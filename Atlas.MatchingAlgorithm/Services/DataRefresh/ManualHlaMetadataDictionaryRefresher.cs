using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    /// <summary>
    /// Recreates the HLA Metadata Dictionary on an operator's request, as opposed to as a stage of a data refresh.
    /// </summary>
    public interface IManualHlaMetadataDictionaryRefresher
    {
        /// <summary>
        /// Recreates the dictionary, unless a data refresh is in progress.
        /// </summary>
        /// <returns>False, having recreated nothing, if a data refresh was in progress.</returns>
        Task<bool> TryRecreate(CreationBehaviour creationBehaviour);
    }

    /// <remarks>
    /// A data refresh recreates the dictionary itself, as its first stage. A manual recreation alongside it would have
    /// two processes rewriting the same tables: each writes a new physical table, and whichever updates the table
    /// reference last wins, orphaning the other. It would also stamp the recreation, making every consuming app -
    /// the refresh's own included - drop its warm cache for that version partway through a long-running refresh.
    ///
    /// <para>
    /// The refresh protects itself from concurrent runs with its lease; this is the equivalent check for the manual
    /// route, and the same one <see cref="DataRefreshRequester"/> makes before starting a refresh. Like that check, it
    /// treats a stalled refresh as in progress - which the stalled-refresh watchdog recovers.
    /// </para>
    ///
    /// <para>
    /// It guards one direction only. A data refresh started while a manual recreation is running is not prevented,
    /// because the manual route holds no lease for it to see; that window is the few minutes a recreation takes.
    /// </para>
    /// </remarks>
    internal class ManualHlaMetadataDictionaryRefresher : IManualHlaMetadataDictionaryRefresher
    {
        private readonly IHlaMetadataDictionaryFactory dictionaryFactory;
        private readonly IActiveHlaNomenclatureVersionAccessor activeVersionAccessor;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        public ManualHlaMetadataDictionaryRefresher(
            IHlaMetadataDictionaryFactory dictionaryFactory,
            IActiveHlaNomenclatureVersionAccessor activeVersionAccessor,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository)
        {
            this.dictionaryFactory = dictionaryFactory;
            this.activeVersionAccessor = activeVersionAccessor;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
        }

        public async Task<bool> TryRecreate(CreationBehaviour creationBehaviour)
        {
            if (dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Any())
            {
                return false;
            }

            var dictionary = dictionaryFactory.BuildDictionary(activeVersionAccessor.GetActiveHlaNomenclatureVersion());
            await dictionary.RecreateHlaMetadataDictionary(creationBehaviour);

            return true;
        }
    }
}
