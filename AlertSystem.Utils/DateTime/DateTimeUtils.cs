namespace AlertSystem.Utils.DateTime
{
    /// <summary>
    /// Date and time utility functions
    /// </summary>
    public static class DateTimeUtils
    {
        /// <summary>
        /// Gets the start of day for a given date
        /// </summary>
        /// <param name="date">Date to process</param>
        /// <returns>Start of day DateTime</returns>
        public static System.DateTime StartOfDay(System.DateTime date)
        {
            return date.Date;
        }

        /// <summary>
        /// Gets the end of day for a given date
        /// </summary>
        /// <param name="date">Date to process</param>
        /// <returns>End of day DateTime</returns>
        public static System.DateTime EndOfDay(System.DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// Checks if a date is today
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <returns>True if date is today, false otherwise</returns>
        public static bool IsToday(System.DateTime date)
        {
            return date.Date == System.DateTime.Today;
        }

        /// <summary>
        /// Gets a human-readable time difference string
        /// </summary>
        /// <param name="dateTime">DateTime to compare</param>
        /// <returns>Human-readable time difference</returns>
        public static string GetTimeAgo(System.DateTime dateTime)
        {
            var timeSpan = System.DateTime.UtcNow - dateTime;

            if (timeSpan.TotalDays > 1)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalHours > 1)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalMinutes > 1)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            
            return "Just now";
        }
    }
}
