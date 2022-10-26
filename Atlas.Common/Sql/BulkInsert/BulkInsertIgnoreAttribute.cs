using System;

namespace Atlas.Common.Sql.BulkInsert
{
    /// <summary>
    /// Apply to those properties that should not be included when bulk inserting.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class BulkInsertIgnoreAttribute : Attribute
    {
    }
}
