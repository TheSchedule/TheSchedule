using System;
using System.Linq;
using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using NUnit.Framework;
using tests.Models;

namespace tests
{
    public class Tests
    {
        private IDockerClient _client { get; set; }
        private CosmosContainer CosmosContainer { get; set; }
        [SetUp]
        public void Setup()
        {
            var uri  = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new Uri("npipe://./pipe/docker_engine") 
                : new Uri("unix:/var/run/docker.sock");
            _client = new DockerClientConfiguration(uri).CreateClient();
            
            TestContext.Progress.WriteLine($"Ensuring network '{DockerContainer.NetworkName}' exists...");
            EnsureIntegrationTestsNetworkExists();

            CosmosContainer = new CosmosContainer(TestContext.Progress, TestContext.Error);
            try { CosmosContainer.Remove(_client).Wait(60*1000); } catch {}
            CosmosContainer.Pull();
            CosmosContainer.Start(_client).Wait(60*1000);
            // Wait for SQL Server container to finish starting
            CosmosContainer.WaitUntilReady().Wait(60*1000);
        }

        private void EnsureIntegrationTestsNetworkExists()
        {
            var networks = _client.Networks.ListNetworksAsync().Result;
            if (!networks.Any(n => n.Name == DockerContainer.NetworkName))
            {
                TestContext.Progress.WriteLine($"⏳ Creating test network '{DockerContainer.NetworkName}'...");
                _client.Networks
                    .CreateNetworkAsync(new NetworksCreateParameters() { Name = DockerContainer.NetworkName })
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


        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}