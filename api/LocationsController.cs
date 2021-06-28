using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using api.Models.Entities;
using System.Collections.Generic;
using api.Data;
using System.Linq;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Cosmos;

namespace api
{
	public static class LocationsController
	{
		[FunctionName(nameof(GetLocations))]
		public static async Task<IActionResult> GetLocations(
			[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "locations")] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("Hit /locations");
			
			var db = await Context.GetDbContainer<Location>(log);
			
			var iterator = db.GetItemLinqQueryable<Location>()
				// .OrderBy(l => l.Active)
				// .ThenBy(l => l.Name)
				.ToFeedIterator();
			
			var locations = new List<Location>();
			using(iterator)
			{
				while(iterator.HasMoreResults)
				{
					foreach(var location in await iterator.ReadNextAsync())
					{
						log.LogInformation($"\tlocation {location.Id}");
						locations.Add(location);
					}
				}
			}
			return new OkObjectResult(locations);
		}

		[FunctionName(nameof(GetLocation))]
		public static async Task<IActionResult> GetLocation([HttpTrigger(AuthorizationLevel.Function, "get", Route = "locations/{locationId}")] HttpRequest request, string locationId, ILogger log)
		{
			log.LogInformation($"Hit /locations/{locationId}");
			try
			{
				var db = await Context.GetDbContainer<Location>(log);

				var pk = new PartitionKey(locationId);
				var response = await db.ReadItemAsync<Location>(locationId, pk);

				return new OkObjectResult(response.Resource);
			}
			catch (Exception ex)
			{
				return new BadRequestObjectResult(ex);
			}
		}

		[FunctionName(nameof(GenerateLocations))]
		public static async Task<IActionResult> GenerateLocations(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "locations/generate")] HttpRequest req,
		ILogger log)
		{
			log.LogInformation("Hit locations/generate");

			var locations = new List<Location>
			{
				new Location
				{
					Id = "e18fe9ae-260d-448a-8f38-6a80a0416662",
					Active = true,
					Name = "Some Name"
				},
				new Location
				{
					Id = "f13bd39f-a2f0-4196-8eb0-727765737009",
					Active = false,
					Name = "Old Place",
					ShortName = "O.P."
				}
			};

			var db = await Context.GetDbContainer<Location>(log);

			foreach(var location in locations)
			{
				log.LogInformation($"Upserting location {location.Id}");
				var resp = await db.UpsertItemAsync<Location>(location);
			}
			log.LogInformation("Done, returning 200.");
			return new OkResult();
		}
	}
}
