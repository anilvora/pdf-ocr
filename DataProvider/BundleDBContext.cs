using Microsoft.EntityFrameworkCore;
using Practice.WebApp.Model;
using System.Reflection.Metadata;

namespace Practice.WebApp.DataProvider
{
	public class BundleDBContext : DbContext
	{
		public BundleDBContext(DbContextOptions<BundleDBContext> options) : base(options)
		{
		}
		public DbSet<PartModel> Parts { get; set; }
		public DbSet<BundleModel> Bundles { get; set; }
		public DbSet<BundlePartModel> BundleParts { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<BundlePartModel>()
				.HasKey(bp => new { bp.BundlePartID });

			modelBuilder.Entity<BundlePartModel>()
				.HasOne(bp => bp.Bundle)
				.WithMany(b => b.BundleParts)
				.HasForeignKey(bp => bp.BundleID);

			modelBuilder.Entity<BundlePartModel>()
				.HasOne(bp => bp.Part)
				.WithMany()
				.HasForeignKey(bp => bp.PartID);
		}
	}
}
