using System;
using Dapper;

namespace Atlas.DonorImport.Data
{
    public static class Initialise
    {
        public static void InitaliseDapper()
        {
            // Without this map, Dapper will map C# DateTime to SQL "dateTime", which doesn't have the necessary precision - and will fail equivalence checks to the original value in C#.
            // "datetime2(7)" does have the required precision, and is how we store datetimes in our database schema.
            SqlMapper.AddTypeMap(typeof(DateTime), System.Data.DbType.DateTime2);
        }
    }
}