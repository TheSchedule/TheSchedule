using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace tests.Models
{
	public class ApiContainer : DockerContainer
	{
		private static System.Net.Http.HttpClient http = new System.Net.Http.HttpClient();
		public string DockerFile { get; }
		public int Port { get; }
		public string CosmosContainerName { get; }

		public ApiContainer(TextWriter progress, TextWriter error, string cosmosContainerName)
			: base(progress, error, "the-schedule-api:test", "the-schedule-api")
		{
			DockerFile = "dockerfile.API";
			Port = 8181;
			CosmosContainerName = cosmosContainerName;
		}

		public void BuildImage()
		{
			Progress.WriteLine($"Copying certificate from {CosmosContainerName}");
			DockerExec($"cp {CosmosContainerName}:/tmp/cosmos/appdata/Packages/DataExplorer/emulator.pem ./api/emulator.crt", "../../../../");
			Progress.WriteLine($"⏳ Building Function App image '{ImageName}'. This can take some time -- hang in there!");
			DockerExec($"build --pull --rm --file {DockerFile} --tag {ImageName} .", "../../../../");
		}

		public override Config ToConfig()
		{
			return new Config
			{
				Env = new List<string> 
				{ 
					//We set the other env strings in Dockerfile.API
					$"CosmosDbUrl=https://{CosmosContainerName}:8081"
				}
			};
		}

		public override HostConfig ToHostConfig()
		{
			return new HostConfig()
			{
				NetworkMode = NetworkName,
				PortBindings = new Dictionary<string, IList<PortBinding>>
				{
					{
						"80/tcp",
						new List<PortBinding>
						{
							new PortBinding
							{
								HostPort = Port.ToString(),
							}
						}
					},
				},
			};
		}

		protected override async Task<bool> isReady()
		{
			try
			{
				var response = await http.GetAsync($"http://localhost:{Port}/api/ping");
				// look for HTTP OK (200) response
				if(response.StatusCode == System.Net.HttpStatusCode.OK)
				{
					return true;
				}
				else
				{
					throw new Exception($"Status Code: {response.StatusCode}");
				}
			}
			catch (Exception e)
			{
				Progress.WriteLine($"🤔 {ContainerName} is not yet ready: {e.Message}");
				return false;
			}
		}
	}
}