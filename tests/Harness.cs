using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using NUnit.Framework;
using tests.Models;

namespace tests
{
	[SetUpFixture]
	public class Harness
	{
		private IDockerClient _client { get; set; }
		private CosmosContainer CosmosContainer { get; set; }
		private ApiContainer ApiContainer { get; set; }

		public Harness()
		{
			var uri  = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? new Uri("npipe://./pipe/docker_engine") 
				: new Uri("unix:/var/run/docker.sock");
			_client = new DockerClientConfiguration(uri).CreateClient();
		}

		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			TestContext.Progress.WriteLine($"Ensuring network '{DockerContainer.NetworkName}' exists...");
			EnsureIntegrationTestsNetworkExists();
			var cosmosIp = GetFirstAvailableIpAddress().Result;
			CosmosContainer = new CosmosContainer(TestContext.Progress, TestContext.Error, cosmosIp);

			try { CosmosContainer.Remove(_client).Wait(60*1000); } catch {}
			CosmosContainer.Pull();
			CosmosContainer.Start(_client).Wait(60*1000);
			// Wait for SQL Server container to finish starting
			CosmosContainer.WaitUntilReady().Wait(60*1000);
			
			ApiContainer = new ApiContainer(TestContext.Progress, TestContext.Error, CosmosContainer.ContainerName);
			try { ApiContainer.Remove(_client).Wait(60*1000); } catch {}
			ApiContainer.BuildImage();
			ApiContainer.Start(_client).Wait(60*1000);
			// Wait for the API to start
			ApiContainer.WaitUntilReady().Wait(60*1000);
		}

		[OneTimeTearDown]
		public void OneTimeTeardown()
		{
			var stopTasks = new List<Task>();
			stopTasks.Add(CosmosContainer.Stop(_client));
			stopTasks.Add(ApiContainer.Stop(_client));
			Task.WhenAll(stopTasks).Wait(60*1000);
		}

		private async Task<string> GetFirstAvailableIpAddress()
		{
			TestContext.Progress.WriteLine($"🔍 Looking for gateway for network '{DockerContainer.NetworkName}'...");
			var networks = await _client.Networks.ListNetworksAsync();
			var network = networks.First(n => n.Name == DockerContainer.NetworkName);
			
			var config = network.IPAM.Config
				.Where(c => string.IsNullOrWhiteSpace(c.Subnet) == false)
				.First();
			
			var ipn = System.Net.IPNetwork.Parse(config.Subnet);
			var firstAddress = ipn.FirstUsable.ToString();
			if(firstAddress == config.Gateway)
			{
				// First usable address is the gateway, find the next address by adding 1 to it's bytes.
				var fuInt = BitConverter.ToUInt32(ipn.FirstUsable.GetAddressBytes());
				firstAddress = ipn.ListIPAddress()
					.First(i => BitConverter.ToUInt32(i.GetAddressBytes()) > fuInt)
					.ToString();
			}
			TestContext.Progress.WriteLine($"😎 '{DockerContainer.NetworkName}' First Address: {firstAddress}");
			return firstAddress;
		}

		private void EnsureIntegrationTestsNetworkExists()
		{
			var networks = _client.Networks.ListNetworksAsync().Result;
			if (!networks.Any(n => n.Name == DockerContainer.NetworkName))
			{
				TestContext.Progress.WriteLine($"⏳ Creating test network '{DockerContainer.NetworkName}'...");
				_client.Networks
					.CreateNetworkAsync(new NetworksCreateParameters() 
					{ 
						Name = DockerContainer.NetworkName,
						IPAM = new IPAM
						{
							Config = new List<IPAMConfig>
							{
								new IPAMConfig
								{
									Subnet = "172.18.0.0/29",// 172.18.0.0 - 172.18.0.7
									Gateway = "172.18.0.1"
								}

							}
						}
					})
					.Wait();
			}
			else
			{
				TestContext.Progress.WriteLine($"😎 Test network '{DockerContainer.NetworkName}' exists.");
			}
			TestContext.Progress.WriteLine($"🔍 Docker Networks (name, driver, scope):");
			foreach (var network in _client.Networks.ListNetworksAsync().Result)
			{
				TestContext.Progress.WriteLine($"  {network.Name}, {network.Driver}, {network.Scope}");
			}
		}
	}
}
