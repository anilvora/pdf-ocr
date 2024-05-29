using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace Practice.WebApp.Model
{
	public class BundlePartModel
	{
		[Key]
		public int BundlePartID { get; set; }

		[ForeignKey("Bundle")]
		public int BundleID { get; set; }
		public BundleModel Bundle { get; set; }

		[ForeignKey("Part")]
		public int PartID { get; set; }
		public PartModel Part { get; set; }

		public int QuantityRequired { get; set; }
	}
}
