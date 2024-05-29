using System.ComponentModel.DataAnnotations;

namespace Practice.WebApp.Model
{
	public class BundleModel
	{
		[Key]
		public int BundleID { get; set; }
		public string BundleName { get; set; }
		public List<BundlePartModel> BundleParts { get; set; }
	}
}
