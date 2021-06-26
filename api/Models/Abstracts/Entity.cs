using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;

namespace api.Models.Abstracts
{
	public abstract class Entity
	{
		[JsonIgnore]
		public string ContainerName => GetContainerName();
		[JsonIgnore]
		public string IdName => GetIdName();
		
		[JsonProperty("id")]
		public string Id { get; set; }

		public bool Active { get; set; }


		public Entity()
		{
			Active = true;
		}

		public static string GetContainerName() => throw new Exception("_GetContainerName() needs override.");
		public static string GetIdName() => throw new Exception("_GetIdName() needs override.");

		public void Validate()
		{
			var validationContext = new ValidationContext(this);
			var results = new List<ValidationResult>();
			if (!Validator.TryValidateObject(this, validationContext, results, true)) // Need to set all properties, otherwise it just checks required.
			{
				var messages = results.Select(r => r.ErrorMessage).ToList().Aggregate((message, nextMessage) => message + ", " + nextMessage);
				throw new Exception($"{this.GetType().FullName} not valid: {messages}");
			}
		}
	}
}