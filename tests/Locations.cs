using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using tests.Models;
using api.Models.Entities;

namespace tests
{
	public static class HttpContentExtensions
	{
		public static async Task<T> ReadAsAsync<T>(this HttpContent content)
		{
			var serializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,     
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(),
                }
            };
			
			var str = await content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<T>(str, serializerSettings);
		}
	}
	
	public class Locations
	{
		private IDockerClient _client { get; set; }
		private CosmosContainer CosmosContainer { get; set; }
		protected static HttpClient Api = new HttpClient(){
			BaseAddress = new System.Uri("http://localhost:8181/api/")
		};

		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public async Task GenerateAndGetLocations()
		{
			var genResp = await Api.GetAsync("Locations/Generate");
			Assert.True(genResp.IsSuccessStatusCode);
			var locationsResp = await Api.GetAsync("Locations");
			Assert.True(locationsResp.IsSuccessStatusCode);
			var result = await locationsResp.Content.ReadAsAsync<List<Location>>();
			Assert.AreEqual(2, result.Count);
		}
	}
}