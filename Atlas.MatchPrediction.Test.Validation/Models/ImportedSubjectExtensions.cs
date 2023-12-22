using Atlas.Client.Models.Search;
using Atlas.ManualTesting.Common.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using System;

namespace Atlas.MatchPrediction.Test.Validation.Models
{
    internal static class ImportSubjectExtensions
    {
        public static SubjectInfo ToSubjectInfo(this ImportedSubject importedSubject, SubjectType subjectType)
        {
            return new SubjectInfo
            {
                ExternalId = importedSubject.ID,
                SubjectType = subjectType,
                DonorType = importedSubject.GetDonorType(subjectType),
                A_1 = importedSubject.A_1,
                A_2 = PositionTwoHla(importedSubject.A_1, importedSubject.A_2),
                B_1 = importedSubject.B_1,
                B_2 = PositionTwoHla(importedSubject.B_1, importedSubject.B_2),
                C_1 = importedSubject.C_1,
                C_2 = PositionTwoHla(importedSubject.C_1, importedSubject.C_2),
                DQB1_1 = importedSubject.DQB1_1,
                DQB1_2 = PositionTwoHla(importedSubject.DQB1_1, importedSubject.DQB1_2),
                DRB1_1 = importedSubject.DRB1_1,
                DRB1_2 = PositionTwoHla(importedSubject.DRB1_1, importedSubject.DRB1_2),
                ExternalHfSetId = GetExternalHfSetId(importedSubject.HF_SET)
            };
        }

        private static DonorType? GetDonorType(this ImportedSubject importedSubject, SubjectType subjectType)
        {
            return importedSubject.DONOR_TYPE?.ToUpper() switch
            {
                "C" => DonorType.Cord,
                "D" => DonorType.Adult,
                null when subjectType == SubjectType.Donor => DonorType.Adult,
                _ => null,
            };
        }

        private static string PositionTwoHla(string positionOne, string positionTwo)
        {
            return string.IsNullOrEmpty(positionTwo) ? positionOne : positionTwo;
        }

        private static int? GetExternalHfSetId(string hfSet)
        {        
            if(string.IsNullOrEmpty(hfSet))
            {
                return null;
            }
            
            if(int.TryParse(hfSet, out var id))
            {
                return id;
            }

            throw new ArgumentException($"{nameof(hfSet)} must be either empty or an integer.", nameof(hfSet));
        }
    }
}