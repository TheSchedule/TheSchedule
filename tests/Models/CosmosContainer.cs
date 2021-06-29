using System.Threading.Tasks;
using System;
using System.IO;
using System.Data.Common;
using Microsoft.Azure.Cosmos;
using System.Net.Http;
using Docker.DotNet.Models;
using System.Collections.Generic;

namespace tests.Models
{
	public class CosmosContainer : DockerContainer
	{
		public CosmosContainer(TextWriter progress, TextWriter error) 
			: base(progress, error, "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator", "the-schedule-cosmos-db")
		{
		}

		public const string DbUrl = "https://127.0.0.1:8081/";
		public const string DbKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
		public const string DbName = "TheSchedule";

		public void Pull()
		{
			DockerExec($"pull {ImageName}", ".");
		}

		public override Config ToConfig() 
			=> new Config
			{
				Env = new List<string> 
				{ 
					"AZURE_COSMOS_EMULATOR_PARTITION_COUNT=3",
					"AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true",
					"AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE=127.0.0.1"
				},
				ExposedPorts = new Dictionary<string, EmptyStruct>
				{
					{
						"8081", new EmptyStruct()
					}
				}
			};

		// Watch the port mapping here to avoid port
		// contention w/ other Sql Server installations
		//-p 8081:8081 -p 10251:10251 -p 10252:10252 -p 10253:10253 -p 10254:10254
		public override HostConfig ToHostConfig() 
			=> new HostConfig()
			{
				//NetworkMode = NetworkName,
				Memory = 2000000000,
				CPUCount = 2,
				PortBindings = new Dictionary<string, IList<PortBinding>>
					{
						{
							"8081/tcp",
							new List<PortBinding>
							{
								new PortBinding
								{
									HostPort = $"8081",
								}
							}
						},
						// {
						// 	"10251/tcp",
						// 	new List<PortBinding>
						// 	{
						// 		new PortBinding
						// 		{
						// 			HostPort = $"10251"
						// 		}
						// 	}
						// },
						// {
						// 	"10252/tcp",
						// 	new List<PortBinding>
						// 	{
						// 		new PortBinding
						// 		{
						// 			HostPort = $"10252"
						// 		}
						// 	}
						// },
						// {
						// 	"10253/tcp",
						// 	new List<PortBinding>
						// 	{
						// 		new PortBinding
						// 		{
						// 			HostPort = $"10253"
						// 		}
						// 	}
						// },
						// {
						// 	"10254/tcp",
						// 	new List<PortBinding>
						// 	{
						// 		new PortBinding
						// 		{
						// 			HostPort = $"10254"
						// 		}
						// 	}
						// },
					},
			};

		// Gotta wait until the database server is really available
		// or you'll get oddball test failures;)
		protected override async Task<bool> isReady()
		{
			try
			{
				var db = await GetDatabase();
				return true;
			}
			catch (Exception e)
			{
				Progress.WriteLine($"🤔 {ContainerName} is not yet ready: {e.Message}");
				return false;
			}
		}

		protected async Task<Database> GetDatabase()
		{
			// Configure the cosmos client to allow the self-signed SSL certificate
			// provided by the Cosmos Emulator.
			var cosmosClientOptions = new CosmosClientOptions()
			{
				HttpClientFactory = () =>
				{
					HttpMessageHandler httpMessageHandler = new HttpClientHandler()
					{
						ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
					};

					return new HttpClient(httpMessageHandler);
				},
				ConnectionMode = ConnectionMode.Gateway
			};
			
			Progress.WriteLine("Getting/Creating Cosmos client...");
			var cosmosClient = new CosmosClient(DbUrl, DbKey, cosmosClientOptions);
			Progress.WriteLine($"Getting/Creating database {DbName}...");
			var dbResp = await cosmosClient.CreateDatabaseIfNotExistsAsync(DbName);
			if(dbResp.StatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new Exception($"Got status code {dbResp.StatusCode}");
			}
			return dbResp.Database;
		}
	}
}