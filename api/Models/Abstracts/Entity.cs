using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace api.Models.Abstracts
{
	public class Entity
	{
		[Key]
		public int Id { get; set; }

		public bool Active { get; set; }

		public Entity()
		{
			Active = true;
		}

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