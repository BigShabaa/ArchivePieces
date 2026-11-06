using ArchivePieces.Models;
using Microsoft.AspNetCore.Identity;

namespace ArchivePieces.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<AppDbContext>();

            // ── Roles ──────────────────────────────────────────────────────────
            foreach (var role in new[] { "Admin", "Customer" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ── Admin user ─────────────────────────────────────────────────────
            var adminEmail = config["AdminSeed:Email"] ?? "admin@archivepieces.com";
            var adminPassword = config["AdminSeed:Password"]
                ?? throw new InvalidOperationException("AdminSeed:Password is not configured.");

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Archive Admin",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ── Categories ─────────────────────────────────────────────────────
            if (!db.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new() { Name = "NEW IN",       Slug = "new-in",       DisplayOrder = 0,  IsVisible = true },
                    new() { Name = "T-SHIRTS",     Slug = "t-shirts",     DisplayOrder = 1,  IsVisible = true },
                    new() { Name = "HOODIES",      Slug = "hoodies",      DisplayOrder = 2,  IsVisible = true },
                    new() { Name = "SWEATSHIRTS",  Slug = "sweatshirts",  DisplayOrder = 3,  IsVisible = true },
                    new() { Name = "JACKETS",      Slug = "jackets",      DisplayOrder = 4,  IsVisible = true },
                    new() { Name = "BOTTOMS",      Slug = "bottoms",      DisplayOrder = 5,  IsVisible = true },
                    new() { Name = "SHORTS",       Slug = "shorts",       DisplayOrder = 6,  IsVisible = true },
                    new() { Name = "HATS",         Slug = "hats",         DisplayOrder = 7,  IsVisible = true },
                    new() { Name = "ACCESSORIES",  Slug = "accessories",  DisplayOrder = 8,  IsVisible = true },
                };
                db.Categories.AddRange(categories);
                await db.SaveChangesAsync();
            }
        }
    }
}
