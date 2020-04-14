using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Common.Repositories
{
    public interface IPGroupRepository
    {
        /// <summary>
        /// Inserts a batch of p groups into storage.
        /// This is only relevant for relational databases (i.e. SQL), where the PGroups are stored separately to the match data.
        /// </summary>
        void InsertPGroups(IEnumerable<string> pGroups);

        int FindOrCreatePGroup(string pGroupName);

        Task<IEnumerable<int>> GetPGroupIds(IEnumerable<string> pGroupNames);
    }
}