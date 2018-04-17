using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Entity;

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

    static class SearchDonorExtensions
    {
        public static Donor ToDonorEntity(this SearchableDonor donor)
        {
            return new Donor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode
            };
        }
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
            return context.Donors.Select(d => d.ToSearchableDonor());
        }

        public SearchableDonor GetDonor(int donorId)
        {
            return context.Donors.FirstOrDefault(d => d.DonorId == donorId)?.ToSearchableDonor();
        }

        public void InsertDonor(SearchableDonor donor)
        {
            context.Donors.AddOrUpdate(donor.ToDonorEntity());
            context.SaveChanges();
        }

        public IEnumerable<PotentialMatch> Search(DonorMatchCriteria matchRequest)
        {
            throw new NotImplementedException();
        }

        public void UpdateDonorWithNewHla(SearchableDonor donor)
        {
            context.Donors.AddOrUpdate(donor.ToDonorEntity());
            context.SaveChanges();
        }
    }
}