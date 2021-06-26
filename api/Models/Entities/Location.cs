using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Abstracts;

namespace api.Models.Entities
{
	public class Location : Entity
	{
		public static new string GetContainerName() => "Locations";
		public static new string GetIdName() => "LocationId";

		[Required]
		[MaxLength(512)]
		public string Name { get; set; }
		
		[NotMapped]
		private string _ShortName { get; set; }

		[MaxLength(8)]
		public string ShortName { 
			get => string.IsNullOrWhiteSpace(_ShortName) ? Name : _ShortName;
			set => _ShortName = value == Name ? null : value;
		}
	}
}