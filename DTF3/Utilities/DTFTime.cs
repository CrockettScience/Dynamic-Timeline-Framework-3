namespace DTF3.Utilities
{
    public static class DTFTimeExt
    {
        private const ulong WEEK = 7;
        private const ulong MONTH = 30;
        private const ulong YEAR = 365;

        private const ulong HUNDRED = 100;
        private const ulong THOUSAND = 1_000;
        private const ulong MILLION = 1_000_000;
        private const ulong BILLION = 1_000_000_000;
        private const ulong TRILLION = 1_000_000_000_000;

        public static ulong Days(this ulong dtfTime)
        {
            return dtfTime;
        }

        public static ulong Weeks(this ulong dtfTime)
        {
            return dtfTime * WEEK;
        }

        public static ulong Months(this ulong dtfTime)
        {
            return dtfTime * MONTH;
        }

        public static ulong Years(this ulong dtfTime)
        {
            return dtfTime * YEAR;
        }

        public static ulong Hundred(this ulong dtfTime)
        {
            return dtfTime * HUNDRED;
        }

        public static ulong Thousand(this ulong dtfTime)
        {
            return dtfTime * THOUSAND;
        }

        public static ulong Million(this ulong dtfTime)
        {
            return dtfTime * MILLION;
        }

        public static ulong Billion(this ulong dtfTime)
        {
            return dtfTime * BILLION;
        }

        public static ulong Trillion(this ulong dtfTime)
        {
            return dtfTime * TRILLION;
        }
    }
}