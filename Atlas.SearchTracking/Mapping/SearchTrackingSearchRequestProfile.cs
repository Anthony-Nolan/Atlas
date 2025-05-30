﻿using Atlas.Debug.Client.Models.SearchTracking;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Data.Models;
using AutoMapper;
using System.Reflection;

namespace Atlas.SearchTracking.Mapping
{
    public class SearchTrackingSearchRequestProfile : Profile
    {
        public SearchTrackingSearchRequestProfile()
        {
            CreateMap<SearchRequest, SearchTrackingSearchRequest>()
                .ForMember(dest => dest.ResultsSent,
                    opt => opt.MapFrom(s => s.ResultsSent))
                .ForMember(dest => dest.ResultsSentTimeUtc,
                    opt => opt.MapFrom(s => s.ResultsSentTimeUtc))
                .ForPath(dest => dest.SearchTrackingMatchPredictionInfo.IsSuccessful,
                    opt => opt.MapFrom(s => s.MatchPrediction_IsSuccessful))
                .ForPath(dest => dest.SearchTrackingMatchPredictionInfo.FailureInfo.Message,
                    opt => opt.MapFrom(s => s.MatchPrediction_FailureInfo_Message))
                .ForPath(dest => dest.SearchTrackingMatchPredictionInfo.FailureInfo.ExceptionStacktrace,
                    opt => opt.MapFrom(s => s.MatchPrediction_FailureInfo_ExceptionStacktrace))
                .ForPath(dest => dest.SearchTrackingMatchPredictionInfo.FailureInfo.Type,
                    opt => opt.MapFrom(s => s.MatchPrediction_FailureInfo_Type))
                .ForPath(dest => dest.SearchTrackingMatchPredictionInfo.DonorsPerBatch,
                    opt => opt.MapFrom(s => s.MatchPrediction_DonorsPerBatch))
                .ForPath(dest => dest.SearchTrackingMatchPredictionInfo.TotalNumberOfBatches,
                    opt => opt.MapFrom(s => s.MatchPrediction_TotalNumberOfBatches))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.IsSuccessful,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_IsSuccessful))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.FailureInfo.Message,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_FailureInfo_Message))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.FailureInfo.ExceptionStacktrace,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_FailureInfo_ExceptionStacktrace))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.FailureInfo.Type,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_FailureInfo_Type))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.TotalAttemptsNumber,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_TotalAttemptsNumber))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.NumberOfResults,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_NumberOfResults))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.RepeatDetails.AddedResultCount,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_RepeatSearch_AddedResultCount))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.RepeatDetails.RemovedResultCount,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_RepeatSearch_RemovedResultCount))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.RepeatDetails.UpdatedResultCount,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_RepeatSearch_UpdatedResultCount))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.HlaNomenclatureVersion,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_HlaNomenclatureVersion))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.ResultsSent,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_ResultsSent))
                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.ResultsSentTimeUtc,
                    opt => opt.MapFrom(s => s.MatchingAlgorithm_ResultsSentTimeUtc))
                .ForMember(dest => dest.SearchRequestMatchPredictionDetails,
                    opt => opt.MapFrom(s => s.SearchRequestMatchPrediction))
                .ForMember(dest => dest.SearchRequestMatchingAlgorithmAttemptDetails, 
                    opt => opt.MapFrom(s => s.SearchRequestMatchingAlgorithmAttempts));

            CreateMap<SearchRequestMatchingAlgorithmAttempts, SearchTrackingMatchingAlgorithmAttemptDetails>()
                .ForPath(dest => dest.AlgorithmCoreMatchingTiming.EndTimeUtc,
                opt => opt.MapFrom(s => s.AlgorithmCore_Matching_EndTimeUtc))
                .ForPath(dest => dest.AlgorithmCoreMatchingTiming.StartTimeUtc,
                opt => opt.MapFrom(s => s.AlgorithmCore_Matching_StartTimeUtc))
                .ForPath(dest => dest.AlgorithmCoreScoringTiming.EndTimeUtc,
                opt => opt.MapFrom(s => s.AlgorithmCore_Scoring_EndTimeUtc))
                .ForPath(dest => dest.AlgorithmCoreScoringTiming.StartTimeUtc,
                opt => opt.MapFrom(s => s.AlgorithmCore_Scoring_StartTimeUtc))
                .ForPath(dest => dest.PersistingResultsTiming.EndTimeUtc,
                opt => opt.MapFrom(s => s.PersistingResults_EndTimeUtc))
                .ForPath(dest => dest.PersistingResultsTiming.StartTimeUtc,
                opt => opt.MapFrom(s => s.PersistingResults_StartTimeUtc))
                .ForPath(dest => dest.FailureInfo.ExceptionStacktrace,
                opt => opt.MapFrom(s => s.FailureInfo_ExceptionStacktrace))
                .ForPath(dest => dest.FailureInfo.Message,
                opt => opt.MapFrom(s => s.FailureInfo_Message))
                .ForPath(dest => dest.FailureInfo.Type,
                opt => opt.MapFrom(s => s.FailureInfo_Type));

            CreateMap<SearchRequestMatchPrediction, SearchTrackingMatchPredictionDetails>()
                .ForPath(dest => dest.AlgorithmCoreRunningBatchesTiming.EndTimeUtc,
                    opt => opt.MapFrom(s => s.AlgorithmCore_RunningBatches_EndTimeUtc))
                .ForPath(dest => dest.AlgorithmCoreRunningBatchesTiming.StartTimeUtc,
                    opt => opt.MapFrom(s => s.AlgorithmCore_RunningBatches_StartTimeUtc))
                .ForPath(dest => dest.PrepareBatchesTiming.EndTimeUtc,
                    opt => opt.MapFrom(s => s.PrepareBatches_EndTimeUtc))
                .ForPath(dest => dest.PrepareBatchesTiming.StartTimeUtc,
                    opt => opt.MapFrom(s => s.PrepareBatches_StartTimeUtc))
                .ForPath(dest => dest.PersistingResultsTiming.EndTimeUtc,
                    opt => opt.MapFrom(s => s.PersistingResults_EndTimeUtc))
                .ForPath(dest => dest.PersistingResultsTiming.StartTimeUtc,
                    opt => opt.MapFrom(s => s.PersistingResults_StartTimeUtc))
                .ForPath(dest => dest.FailureInfo.ExceptionStacktrace,
                    opt => opt.MapFrom(s => s.FailureInfo_ExceptionStacktrace))
                .ForPath(dest => dest.FailureInfo.Message,
                    opt => opt.MapFrom(s => s.FailureInfo_Message))
                .ForPath(dest => dest.FailureInfo.Type,
                    opt => opt.MapFrom(s => s.FailureInfo_Type));

            CreateMap<MatchingAlgorithmFailureType, SearchTrackingMatchingAlgorithmFailureType>();

            CreateMap<MatchPredictionFailureType, SearchTrackingMatchPredictionFailureType>();
        }
    }
}