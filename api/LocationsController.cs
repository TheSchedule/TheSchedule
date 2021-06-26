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

namespace api
{
	public static class LocationsController
	{
		[FunctionName(nameof(GetLocations))]
		public static async Task<IActionResult> GetLocations(
			[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "locations")] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("C# HTTP trigger function processed a request.");

			string name = req.Query["name"];

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
			dynamic data = JsonConvert.DeserializeObject(requestBody);
			name = name ?? data?.name;

			string responseMessage = string.IsNullOrEmpty(name)
				? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
				: $"Hello, {name}. This HTTP triggered function executed successfully.";
			var result = new List<Location>
			{
				new Location
				{
					Id = 15,
					Active = true,
					Name = string.IsNullOrWhiteSpace(name) ? "Some Name" : name
				},
				new Location
				{
					Id = 1,
					Active = false,
					Name = "Old Place",
					ShortName = "O.P."
				}
			};
			return new OkObjectResult(result);
		}
	}
}
