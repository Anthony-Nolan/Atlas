using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace Nova.SearchAlgorithm.Data.Persistent.Extensions
{
    public static class DbSetExtensions
    {
        /// <summary>
        /// Creates an entity in the DbSet if predicate returns false.
        /// If true, no entity will be added/updated
        /// </summary>
        public static T AddIfNotExists<T>(this DbSet<T> dbSet, T entity, Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            var exists = predicate != null ? dbSet.Any(predicate) : dbSet.Any();
            return !exists ? dbSet.Add(entity) : null;
        }
    }
}