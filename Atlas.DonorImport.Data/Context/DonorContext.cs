﻿using Atlas.DonorImport.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.DonorImport.Data.Context
{
    public class DonorContext : DbContext
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public DonorContext(DbContextOptions<DonorContext> options) : base(options)
        {       
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Donor>().SetUpDonorModel();
        }

        public DbSet<Donor> Donors { get; set; }
    }
}
