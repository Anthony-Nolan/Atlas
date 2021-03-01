using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.RepeatSearch.Data.Models
{
    [Table(TableName)]
    public class RepeatSearchHistoryRecord
    {
        private const string TableName = "RepeatSearchHistoryRecords";
        
        public int Id { get; set; }
        
        [MaxLength(200)]
        [Required]
        public string OriginalSearchRequestId { get; set; }
        
        [MaxLength(200)]
        [Required]
        public string RepeatSearchRequestId { get; set; }
        
        public DateTimeOffset SearchCutoffDate { get; set; }
        
        public DateTimeOffset DateCreated { get; set; } 
    }
}