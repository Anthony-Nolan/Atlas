using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Models;

namespace Atlas.ManualTesting.Services.WmdaConsensusResults
{
    public interface IWmdaResultsTotalMismatchComparer : IWmdaResultsComparer { }
    internal class WmdaResultsTotalMismatchComparer : WmdaResultsComparerBase<WmdaConsensusResultsFile>, IWmdaResultsTotalMismatchComparer
    {
        public WmdaResultsTotalMismatchComparer(
            IFileReader<WmdaConsensusResultsFile> resultsFileReader,
            IFileReader<ImportedSubject> subjectFileReader) 
            : base(resultsFileReader, subjectFileReader)
        {
        }

        protected override IDictionary<Locus, string> SelectLocusMismatchCounts(WmdaConsensusResultsFile results)
        {
            return results.TotalMismatchCounts;
        }
    }

    public interface IWmdaResultsAntigenMismatchComparer : IWmdaResultsComparer { }
    internal class WmdaResultsAntigenMismatchComparer : WmdaResultsComparerBase<WmdaConsensusResultsFileSetTwo>, IWmdaResultsAntigenMismatchComparer
    {
        public WmdaResultsAntigenMismatchComparer(
            IFileReader<WmdaConsensusResultsFileSetTwo> resultsFileReader,
            IFileReader<ImportedSubject> subjectFileReader) 
            : base(resultsFileReader, subjectFileReader)
        {
        }

        protected override IDictionary<Locus, string> SelectLocusMismatchCounts(WmdaConsensusResultsFileSetTwo results)
        {
            return results.AntigenMismatchCounts;
        }
    }
}