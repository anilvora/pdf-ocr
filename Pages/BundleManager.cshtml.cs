using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Practice.WebApp.DataProvider;

namespace Practice.WebApp.Pages
{
    public class BundleManagerModel : PageModel
	{

		private readonly BundleDBContext _context;

		public BundleManagerModel(BundleDBContext context)
		{
			_context = context;
		}
		public int MaxBikes { get; private set; }

		public void OnGet()
		{
			MaxBikes = ComputeMaxBikes();
            var Test = MaxBikes;
		}
        private int ComputeMaxBikes()
        {
            int maxBikes = int.MaxValue;

            // Get the required parts and quantities for the bike bundle
            Dictionary<int, int> requiredParts = GetRequiredParts("Bike");

            // Compute the maximum number of bikes based on the available parts inventory
            foreach (KeyValuePair<int, int> kvp in requiredParts)
            {
                int partID = kvp.Key;
                int quantityRequired = kvp.Value;

                int availableInventory = GetAvailableInventory(partID);
                int possibleBikes = availableInventory / quantityRequired;

                if (possibleBikes < maxBikes)
                {
                    maxBikes = possibleBikes;
                }
            }

            return maxBikes;
        }

        private Dictionary<int, int> GetRequiredParts(string bundleName)
        {
            var bundle = _context.Bundles
                .Include(b => b.BundleParts)
                .ThenInclude(bp => bp.Part)
                .FirstOrDefault(b => b.BundleName == bundleName);

            if (bundle == null)
            {
                throw new InvalidOperationException("Bundle not found");
            }

            return bundle.BundleParts.ToDictionary(bp => bp.PartID, bp => bp.QuantityRequired);
        }

        private int GetAvailableInventory(int partID)
        {
            var part = _context.Parts.FirstOrDefault(p => p.PartID == partID);

            if (part == null)
            {
                throw new InvalidOperationException("Part not found");
            }

            return part.InventoryCount;
        }
    }
}
