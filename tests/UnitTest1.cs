using System;
using System.Runtime.InteropServices;
using Docker.DotNet;
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
            
            CosmosContainer = new CosmosContainer(TestContext.Progress, TestContext.Error);
            try { CosmosContainer.Remove(_client).Wait(60*1000); } catch {}
            CosmosContainer.Pull();
            CosmosContainer.Start(_client).Wait(60*1000);
            // Wait for SQL Server container to finish starting
            CosmosContainer.WaitUntilReady().Wait(60*1000);
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}