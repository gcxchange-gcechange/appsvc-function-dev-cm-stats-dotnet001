using Microsoft.Extensions.Configuration;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    internal class Config
    {
        private IConfiguration _Config;

        public string AzureWebJobsStorage { get { return _Config["AzureWebJobsStorage"]; } }
        public string ClientId { get { return _Config["clientId"]; } }
        public string ContainerName { get { return _Config["containerName"]; } }
        public string PropertyId { get { return _Config["propertyId"]; } }
        public string KeyVaultUrl { get { return _Config["keyVaultUrl"]; } }
        public string ListId { get { return _Config["listId"]; } }
        public string PrivateKeySecretName { get { return _Config["privateKeySecretName"]; } }
        public string SecretName { get { return _Config["secretName"]; } }
        public string SecretNamePassword { get { return _Config["delegatedUserSecret"]; } }
        public string SiteId { get { return _Config["siteId"]; } }
        public string SkillsNameFr { get { return _Config["skillsNameFr"]; } }
        public string TenantId { get { return _Config["tenantId"]; } }
        public string Username { get { return _Config["delegatedUserName"]; } }

        // google api credentials
        public string type { get { return _Config["type"]; } }
        public string project_id { get { return _Config["project_id"]; } }
        public string private_key_id { get { return _Config["private_key_id"]; } }
        public string client_email { get { return _Config["client_email"]; } }
        public string client_id { get { return _Config["client_id"]; } }
        public string auth_uri { get { return _Config["auth_uri"]; } }
        public string token_uri { get { return _Config["token_uri"]; } }
        public string auth_provider_x509_cert_url { get { return _Config["auth_provider_x509_cert_url"]; } }
        public string client_x509_cert_url { get { return _Config["client_x509_cert_url"]; } }
        public string universe_domain { get { return _Config["universe_domain"]; } }

        public Config()
        {
            _Config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();
        }
    }
}
