using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Data.Persistent.Extensions;
using Nova.SearchAlgorithm.Data.Persistent.Models.ScoringWeightings;

namespace Nova.SearchAlgorithm.Data.Persistent.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<SearchAlgorithmPersistentContext>
    {
        private const int DefaultWeight = 0;

        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "Nova.SearchAlgorithm.Data.Persistent.SearchAlgorithmPersistentContext";
            CommandTimeout = 0;
        }

        // This method will be called after migrating to the latest version.
        // Note that it is run *every time* Update-Database is called, not just when a new migration is applied
        protected override void Seed(SearchAlgorithmPersistentContext context)
        {
            SeedGradeWeightings(context);
            SeedConfidenceWeightings(context);
            context.SaveChanges();
        }

        private static void SeedGradeWeightings(DbContext context)
        {
            var grades = Enum.GetValues(typeof(MatchGrade)).Cast<MatchGrade>();
            foreach (var grade in grades)
            {
                var weighting = new GradeWeighting {Name = grade.ToString(), Weight = DefaultWeight};
                context.Set<GradeWeighting>().AddIfNotExists(weighting, w => w.Name == grade.ToString());
            }
        }

        private static void SeedConfidenceWeightings(DbContext context)
        {
            var confidences = Enum.GetValues(typeof(MatchConfidence)).Cast<MatchConfidence>();
            foreach (var confidence in confidences)
            {
                var weighting = new ConfidenceWeighting() {Name = confidence.ToString(), Weight = DefaultWeight};
                context.Set<ConfidenceWeighting>().AddIfNotExists(weighting, w => w.Name == confidence.ToString());
            }
        }
    }
}