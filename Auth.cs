﻿using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Graph;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    internal class Auth
    {
        public static GraphServiceClient GetClient(ILogger logger)
        {
            ROPCConfidentialTokenCredential auth = new ROPCConfidentialTokenCredential(logger);
            return new GraphServiceClient(auth);
        }

        public class ROPCConfidentialTokenCredential : Azure.Core.TokenCredential
        {
            string _clientId;
            string _clientSecret;
            string _password;
            string _tenantId;
            string _tokenEndpoint;
            string _username;
            ILogger _log;

            public ROPCConfidentialTokenCredential(Microsoft.Extensions.Logging.ILogger log)
            {
                Config config = new Config();

                _clientId = config.ClientId;
                _tenantId = config.TenantId;
                _username = config.Username;
                _log = log;
                _tokenEndpoint = "https://login.microsoftonline.com/" + _tenantId + "/oauth2/v2.0/token";

                try
                {
                    SecretClientOptions options = new SecretClientOptions()
                    {
                        Retry =
                        {
                            Delay= TimeSpan.FromSeconds(2),
                            MaxDelay = TimeSpan.FromSeconds(16),
                            MaxRetries = 5,
                            Mode = RetryMode.Exponential
                        }
                    };
                    var client = new SecretClient(new Uri(config.KeyVaultUrl), new DefaultAzureCredential(), options);

                    KeyVaultSecret secret = client.GetSecret(config.SecretName);
                    _clientSecret = secret.Value;

                    KeyVaultSecret password = client.GetSecret(config.SecretNamePassword);
                    _password = password.Value;
                }
                catch (Exception e)
                {
                    _log.LogError("Error accessing the KeyVault!!");
                    _log.LogError(e.Message);
                    _log.LogError(e.StackTrace);
                }
            }

            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                HttpClient httpClient = new HttpClient();

                // Create the request body
                var Parameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("scope", string.Join(" ", requestContext.Scopes)),
                    new KeyValuePair<string, string>("username", _username),
                    new KeyValuePair<string, string>("password", _password),
                    new KeyValuePair<string, string>("grant_type", "password")
                };

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(Parameters)
                };

                var response = httpClient.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
                dynamic responseJson = JsonConvert.DeserializeObject(response);
                var expirationDate = DateTimeOffset.UtcNow.AddMinutes(60.0);
                return new AccessToken(responseJson.access_token.ToString(), expirationDate);
            }

            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                HttpClient httpClient = new HttpClient();

                // Create the request body
                var Parameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("scope", string.Join(" ", requestContext.Scopes)),
                    new KeyValuePair<string, string>("username", _username),
                    new KeyValuePair<string, string>("password", _password),
                    new KeyValuePair<string, string>("grant_type", "password")
                };

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(Parameters)
                };

                var response = httpClient.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
                dynamic responseJson = JsonConvert.DeserializeObject(response);
                var expirationDate = DateTimeOffset.UtcNow.AddMinutes(60.0);
                return new ValueTask<AccessToken>(new AccessToken(responseJson.access_token.ToString(), expirationDate));
            }
        }




    }
}
