using System.Configuration;

namespace appsvc_function_dev_cm_stats_dotnet001.Configuration
{
    public class Report : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
        }

        [ConfigurationProperty("metrics", IsRequired = true)]
        public string Metrics
        {
            get
            {
                return this["metrics"] as string;
            }
        }

        [ConfigurationProperty("dimensions", IsRequired = true)]
        public string Dimensions
        {
            get
            {
                return this["dimensions"] as string;
            }
        }


        [ConfigurationProperty("filter", IsRequired = true)]
        public string Filter
        {
            get
            {
                return this["filter"] as string;
            }
        }


    }
}
