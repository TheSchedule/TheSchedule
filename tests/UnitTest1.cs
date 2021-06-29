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
        }

        


        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}