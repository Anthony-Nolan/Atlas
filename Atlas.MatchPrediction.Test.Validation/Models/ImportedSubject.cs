﻿
// ReSharper disable InconsistentNaming

using Atlas.MatchPrediction.Test.Validation.Data.Models;

namespace Atlas.MatchPrediction.Test.Validation.Models
{
    internal class ImportedSubject
    {
        public string ID { get; set; }
        public string A_1 { get; set; }
        public string A_2 { get; set; }
        public string B_1 { get; set; }
        public string B_2 { get; set; }
        public string C_1 { get; set; }
        public string C_2 { get; set; }
        public string DQB1_1 { get; set; }
        public string DQB1_2 { get; set; }
        public string DRB1_1 { get; set; }
        public string DRB1_2 { get; set; }
    }

    internal static class ImportSubjectExtensions
    {
        public static SubjectInfo ToSubjectInfo(this ImportedSubject importedSubject, SubjectType subjectType)
        {
            return new SubjectInfo
            {
                ExternalId = importedSubject.ID,
                SubjectType = subjectType,
                A_1 = importedSubject.A_1,
                A_2 = importedSubject.A_2,
                B_1 = importedSubject.B_1,
                B_2 = importedSubject.B_2,
                C_1 = importedSubject.C_1,
                C_2 = importedSubject.C_2,
                DQB1_1 = importedSubject.DQB1_1,
                DQB1_2 = importedSubject.DQB1_2,
                DRB1_1 = importedSubject.DRB1_1,
                DRB1_2 = importedSubject.DRB1_2
            };
        }
    }
}