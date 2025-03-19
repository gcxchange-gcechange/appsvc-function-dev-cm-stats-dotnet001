using System.Globalization;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    internal class Common
    {
        public static string GetFileName(string reportName)
        {
            Config config = new Config();
            return string.Format("GA4Report_{0}_{1}_{2}.json", config.PropertyId, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture), reportName);
        }
    }
}