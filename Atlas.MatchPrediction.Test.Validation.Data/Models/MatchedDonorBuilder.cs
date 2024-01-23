using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Validation.Data.Models
{
    internal static class MatchedDonorBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<MatchedDonor> modelBuilder)
        {
            modelBuilder
                .HasOne<ValidationSearchRequestRecord>()
                .WithMany()
                .HasForeignKey(r => r.SearchRequestRecord_Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasIndex(r => new { r.SearchRequestRecord_Id })
                .IncludeProperties(r => new { 
                    r.DonorCode, 
                    r.TotalMatchCount, 
                    r.PatientHfSetPopulationId, 
                    r.DonorHfSetPopulationId, 
                    r.WasPatientRepresented, 
                    r.WasDonorRepresented });
        }
    }
}