using System;
using System.Net.Http;
using System.Threading.Tasks;
using api.Models.Abstracts;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace api.Data
{
	public class Context
	{
		public static async Task<Container> GetDbContainer<T> (ILogger logger) where T : Entity
		{
			var cosmosDbUrl = GetEnv("CosmosDbUrl", true);
			var cosmosDbKey = GetEnv("CosmosDbKey", true);
			var cosmosDbId = GetEnv("CosmosDbId", true);

			// If we're on local we want to configure the CosmosClient to accept self-signt SSL certs
			bool.TryParse(GetEnv("CosmosTrustAllCerts", false), out bool cosmosTrustAllCerts);
			var cosmosClientOptions = new CosmosClientOptions()
			{
				HttpClientFactory = () =>
				{
					HttpMessageHandler httpMessageHandler = new HttpClientHandler();

					if(cosmosTrustAllCerts)
					{
						logger.LogWarning("Trusting all certs.");
						httpMessageHandler = new HttpClientHandler()
						{
							ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
						};
					}

					return new HttpClient(httpMessageHandler);
				},
				ConnectionMode = ConnectionMode.Gateway
			};
			
			// Use reflection to get the ContainerName and IdName
			var type = typeof(T);
			var containerName = (string)type.GetMethod(nameof(Entity.GetContainerName)).Invoke(null, null);

			logger.LogInformation($"{cosmosDbUrl}\n{cosmosDbKey}\n{cosmosDbId}\n{containerName}\n{cosmosTrustAllCerts}");
			
			var cosmosClient = new CosmosClient(cosmosDbUrl, cosmosDbKey);
			logger.LogInformation("Created client.");
			var dbResp = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDbId);
			logger.LogInformation($"Created database {cosmosDbId}");
			var db = dbResp.Database;

			var containerResp = await db.CreateContainerIfNotExistsAsync(containerName, "/id");
			logger.LogInformation($"Created container {containerName}");
			var container = containerResp.Container;

			return container;
		}

		private static string GetEnv(string key, bool required)
		{
			string result = Environment.GetEnvironmentVariable(key);

			if(required && string.IsNullOrWhiteSpace(key))
			{
				throw new Exception($"Could not find a value for key '{key}' in appsettings file.");
			}

			return result;
		}
	}
}