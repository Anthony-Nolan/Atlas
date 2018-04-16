using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using RefactorThis.GraphDiff;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public interface IDonorMatchRepository
    {
        void InsertDonor(SearchableDonor donor);
        void UpdateDonorWithNewHla(SearchableDonor donor);
        SearchableDonor GetDonor(int donorId);
        IEnumerable<SearchableDonor> AllDonors();
        IEnumerable<PotentialMatch> Search(DonorMatchCriteria matchRequest);
    }

    public class SqlDonorMatchRepository : IDonorMatchRepository
    {
        private readonly SearchAlgorithmContext context;

        public SqlDonorMatchRepository(SearchAlgorithmContext context)
        {
            this.context = context;
        }

        public IEnumerable<SearchableDonor> AllDonors()
        {
            throw new NotImplementedException();
        }

        public SearchableDonor GetDonor(int donorId)
        {
            throw new NotImplementedException();
        }

        public void InsertDonor(SearchableDonor donor)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PotentialMatch> Search(DonorMatchCriteria matchRequest)
        {
            throw new NotImplementedException();
        }

        public void UpdateDonorWithNewHla(SearchableDonor donor)
        {
            throw new NotImplementedException();
        }
    }
}