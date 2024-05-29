using System.ComponentModel.DataAnnotations;

namespace Practice.WebApp.Model
{
	public class PartModel
	{
		[Key]
		public int PartID { get; set; }
		public string PartName { get; set; }
		public int InventoryCount { get; set; }
	}
}
