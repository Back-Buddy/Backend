using Google.Cloud.Firestore;

namespace BackBuddy.Core.Library.Database.Firebase
{
    public static class StartsWithExtension
    {
        /// <summary>
        /// Adds a "starts with" condition to the query for the specified field and value.
        /// </summary>
        /// <remarks>
        /// Constructs a case-sensitive query that filters documents where the specified field starts with the given value.
        /// </remarks>
        /// <param name="field">The name of the field to filter on.</param>
        /// <param name="value">The value that the field should start with (case-sensitive).</param>
        public static Query StartsWith(this Query query, string field, string value)
        {
            return query
                .WhereGreaterThanOrEqualTo(field, value)
                .WhereLessThan(field, value + "\uf8ff");
        }

    }
}
