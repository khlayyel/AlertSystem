namespace AlertSystem.Utils.Collections
{
    /// <summary>
    /// Collection utility functions
    /// </summary>
    public static class CollectionUtils
    {
        /// <summary>
        /// Checks if a collection is null or empty
        /// </summary>
        /// <typeparam name="T">Type of collection elements</typeparam>
        /// <param name="collection">Collection to check</param>
        /// <returns>True if null or empty, false otherwise</returns>
        public static bool IsNullOrEmpty<T>(IEnumerable<T>? collection)
        {
            return collection == null || !collection.Any();
        }

        /// <summary>
        /// Safely gets the first element of a collection or returns default
        /// </summary>
        /// <typeparam name="T">Type of collection elements</typeparam>
        /// <param name="collection">Collection to process</param>
        /// <returns>First element or default value</returns>
        public static T? FirstOrDefaultSafe<T>(IEnumerable<T>? collection) where T : class
        {
            return collection?.FirstOrDefault();
        }

        /// <summary>
        /// Safely gets the count of a collection
        /// </summary>
        /// <typeparam name="T">Type of collection elements</typeparam>
        /// <param name="collection">Collection to count</param>
        /// <returns>Count or 0 if null</returns>
        public static int CountSafe<T>(IEnumerable<T>? collection)
        {
            return collection?.Count() ?? 0;
        }
    }
}
