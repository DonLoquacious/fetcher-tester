namespace fetcher_tester
{
    public static class AppConfig
    {
        // pseudo-readonly configuration singleton, for simplicity
        public static IConfiguration? Configuration { get; private set; }

        public static bool SetConfiguration(IConfiguration configuration)
        {
            if (Configuration == null)
            {
                Configuration = configuration;
                return true;
            }

            return false;
        }

        public static string? GetConfigValue(string key)
        {
            return Configuration?[key];
        }

        public static string? GetConfigValue(string key, string? @default = null)
        {
            return Configuration?[key] ?? @default;
        }

        public static int GetConfigValue(string key, int @default = 0)
        {
            if (int.TryParse(Configuration?[key], out int p))
                return p;

            return @default;
        }

        public static double GetConfigValue(string key, double @default = 0.0d)
        {
            if (double.TryParse(Configuration?[key], out double p))
                return p;

            return @default;
        }

        public static bool GetConfigValue(string key, bool @default = false)
        {
            if (bool.TryParse(Configuration?[key], out bool p))
                return p;

            return @default;
        }
    }
}
