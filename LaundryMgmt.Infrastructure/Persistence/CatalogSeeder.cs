using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LaundryMgmt.Infrastructure.Persistence;

/// <summary>
/// Dev-only bootstrap (mirrors <see cref="Identity.IdentitySeeder"/>'s shape): seeds all
/// 12 starter service categories with a realistic, comprehensive set of services/garments/
/// prices/add-ons — one or more services per category, each with several garment items at
/// realistic INR prices — so the new Category → Service → Item hierarchy has real content
/// to browse immediately instead of a token example. Idempotent — skips entirely if any
/// category already exists. Admins refine/extend the real catalog via the admin screens.
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(CatalogSeeder));

        if (await db.ServiceCategories.AnyAsync())
            return;

        // ---- Categories ----
        string[] categoryNames =
        {
            "Wash & Laundry", "Ironing & Pressing", "Dry Cleaning", "Stain Removal",
            "Shoes & Footwear", "Bags & Accessories", "Bedding & Blankets",
            "Curtains & Home Furnishing", "Wedding & Traditional Wear", "Leather & Suede",
            "Alteration & Repair", "Commercial / B2B Laundry"
        };
        var categories = categoryNames
            .Select((name, index) => new ServiceCategory { Name = name, DisplayOrder = index })
            .ToList();
        db.ServiceCategories.AddRange(categories);
        ServiceCategory Cat(string name) => categories.First(c => c.Name == name);

        // ---- Garments (shared pool — the same item can be priced under many services) ----
        string[] garmentNames =
        {
            "Cotton Shirt", "T-Shirt", "Cotton Pant", "Jeans", "Shorts", "Kurta",
            "Saree", "Silk Saree", "Designer Saree", "Lehenga", "Sherwani", "Salwar Suit",
            "Blazer", "Suit (2-Piece)", "Wedding Dress", "Bridal Dress",
            "Single Bedsheet", "Bedsheet (Double)", "Pillow Cover", "Blanket", "Comforter",
            "Curtains (per panel)", "Sofa Cover", "Cushion Cover", "Table Cover", "Carpet", "Rug",
            "Sneakers", "Formal Shoes", "Leather Shoes", "Boots", "Sandals",
            "Handbag", "Backpack", "Wallet", "Travel Bag",
            "Leather Jacket", "Leather Coat", "Suede Jacket",
            "Hotel Bedsheet", "Hotel Towel", "Corporate Uniform", "Chef Uniform"
        };
        var garments = garmentNames.ToDictionary(n => n, n => new Garment { Name = n });
        db.Garments.AddRange(garments.Values);
        Garment G(string name) => garments[name];

        var allServices = new List<Service>();
        var allPrices = new List<GarmentServicePrice>();

        Service NewService(
            string name, ServiceCategory category, decimal basePrice, int etaHours, decimal gst, int priority,
            decimal expressSurcharge, int expressEtaHours)
        {
            var service = new Service
            {
                Name = name, Category = category, BasePrice = basePrice, EstimatedTimeHours = etaHours,
                GstPercentage = gst, Priority = priority, ExpressSurcharge = expressSurcharge, ExpressEtaHours = expressEtaHours
            };
            allServices.Add(service);
            return service;
        }

        void Price(Service service, string garmentName, decimal price, PricingType type = PricingType.PerItem)
        {
            allPrices.Add(new GarmentServicePrice { Garment = G(garmentName), Service = service, PricingType = type, Price = price });
        }

        // ---- 1. Wash & Laundry ----
        var washLaundry = Cat("Wash & Laundry");
        var regularWash = NewService("Regular Wash", washLaundry, 22, 24, 5, 1, 12, 8);
        Price(regularWash, "Cotton Shirt", 22); Price(regularWash, "T-Shirt", 18);
        Price(regularWash, "Cotton Pant", 28); Price(regularWash, "Jeans", 35); Price(regularWash, "Shorts", 20);

        var washFold = NewService("Wash & Fold", washLaundry, 20, 24, 5, 2, 20, 8);
        Price(washFold, "Cotton Shirt", 20); Price(washFold, "T-Shirt", 18);
        Price(washFold, "Bedsheet (Double)", 55, PricingType.WeightBased);

        var expressWash = NewService("Express Wash", washLaundry, 45, 6, 5, 3, 0, 4);
        Price(expressWash, "Cotton Shirt", 45); Price(expressWash, "T-Shirt", 38); Price(expressWash, "Cotton Pant", 55);

        // ---- 2. Ironing & Pressing ----
        var ironing = Cat("Ironing & Pressing");
        var steamIron = NewService("Steam Iron", ironing, 30, 24, 5, 1, 15, 6);
        Price(steamIron, "Cotton Shirt", 30); Price(steamIron, "Cotton Pant", 35);
        Price(steamIron, "T-Shirt", 25); Price(steamIron, "Kurta", 40); Price(steamIron, "Saree", 80);
        Price(steamIron, "Salwar Suit", 45);

        var expressIroning = NewService("Express Ironing", ironing, 50, 6, 5, 2, 0, 4);
        Price(expressIroning, "Cotton Shirt", 50); Price(expressIroning, "Cotton Pant", 60); Price(expressIroning, "Saree", 120);

        var heavyPress = NewService("Heavy Press", ironing, 100, 24, 5, 3, 40, 10);
        Price(heavyPress, "Saree", 100); Price(heavyPress, "Suit (2-Piece)", 150); Price(heavyPress, "Blazer", 130);

        // ---- 3. Dry Cleaning ----
        var dryCleaning = Cat("Dry Cleaning");
        var dryClean = NewService("Dry Clean", dryCleaning, 100, 48, 12, 1, 50, 24);
        Price(dryClean, "Cotton Shirt", 90); Price(dryClean, "Cotton Pant", 100);
        Price(dryClean, "Suit (2-Piece)", 250);
        // Item-level override, proving the fallback-chain plumbing: this blazer's express
        // price and GST are set explicitly rather than falling back to Service defaults.
        allPrices.Add(new GarmentServicePrice { Garment = G("Blazer"), Service = dryClean, PricingType = PricingType.PerItem, Price = 150, ExpressPrice = 220, GstPercentage = 18 });
        Price(dryClean, "Saree", 180);

        var premiumDryClean = NewService("Premium Dry Clean", dryCleaning, 350, 72, 18, 2, 100, 36);
        Price(premiumDryClean, "Silk Saree", 350); Price(premiumDryClean, "Designer Saree", 500); Price(premiumDryClean, "Sherwani", 400);

        var expressDryClean = NewService("Express Dry Clean", dryCleaning, 150, 24, 12, 3, 0, 12);
        Price(expressDryClean, "Cotton Shirt", 150); Price(expressDryClean, "Suit (2-Piece)", 400);

        // ---- 4. Stain Removal ----
        var stainRemovalCat = Cat("Stain Removal");
        var stainTreatment = NewService("Stain Treatment", stainRemovalCat, 60, 24, 5, 1, 25, 12);
        Price(stainTreatment, "Cotton Shirt", 40); Price(stainTreatment, "Cotton Pant", 45);
        Price(stainTreatment, "Saree", 80); Price(stainTreatment, "Blazer", 90); Price(stainTreatment, "Suit (2-Piece)", 120);

        // ---- 5. Shoes & Footwear ----
        var footwear = Cat("Shoes & Footwear");
        var shoeCleaning = NewService("Shoe Cleaning", footwear, 150, 48, 12, 1, 60, 24);
        Price(shoeCleaning, "Sneakers", 150); Price(shoeCleaning, "Formal Shoes", 180); Price(shoeCleaning, "Sandals", 100);

        var deepShoeCleaning = NewService("Deep Shoe Cleaning", footwear, 250, 48, 12, 2, 80, 24);
        Price(deepShoeCleaning, "Sneakers", 250); Price(deepShoeCleaning, "Leather Shoes", 300); Price(deepShoeCleaning, "Boots", 320);

        // ---- 6. Bags & Accessories ----
        var bags = Cat("Bags & Accessories");
        var handbagCleaning = NewService("Handbag Cleaning", bags, 300, 48, 12, 1, 100, 24);
        Price(handbagCleaning, "Handbag", 300); Price(handbagCleaning, "Backpack", 200); Price(handbagCleaning, "Wallet", 100);

        var travelBagCleaning = NewService("Travel Bag Cleaning", bags, 350, 48, 12, 2, 100, 24);
        Price(travelBagCleaning, "Travel Bag", 350);

        // ---- 7. Bedding & Blankets ----
        var bedding = Cat("Bedding & Blankets");
        var bedsheetWash = NewService("Bedsheet Wash", bedding, 50, 24, 5, 1, 20, 8);
        Price(bedsheetWash, "Single Bedsheet", 50); Price(bedsheetWash, "Pillow Cover", 30);

        var blanketWash = NewService("Blanket Wash", bedding, 180, 48, 5, 2, 60, 24);
        Price(blanketWash, "Blanket", 180, PricingType.WeightBased);

        var blanketDryClean = NewService("Blanket Dry Clean", bedding, 400, 48, 12, 3, 100, 24);
        Price(blanketDryClean, "Comforter", 400);

        // ---- 8. Curtains & Home Furnishing ----
        var curtains = Cat("Curtains & Home Furnishing");
        var curtainWash = NewService("Curtain & Upholstery Wash", curtains, 150, 48, 5, 1, 50, 24);
        Price(curtainWash, "Curtains (per panel)", 150); Price(curtainWash, "Sofa Cover", 200);
        Price(curtainWash, "Cushion Cover", 60); Price(curtainWash, "Table Cover", 80);

        var carpetCleaning = NewService("Carpet Cleaning", curtains, 500, 72, 12, 2, 150, 48);
        Price(carpetCleaning, "Carpet", 500); Price(carpetCleaning, "Rug", 300);

        // ---- 9. Wedding & Traditional Wear ----
        var wedding = Cat("Wedding & Traditional Wear");
        var weddingDryClean = NewService("Wedding Dry Clean", wedding, 600, 96, 18, 1, 150, 48);
        Price(weddingDryClean, "Lehenga", 600); Price(weddingDryClean, "Sherwani", 500);
        Price(weddingDryClean, "Wedding Dress", 800); Price(weddingDryClean, "Bridal Dress", 900);
        Price(weddingDryClean, "Salwar Suit", 350);

        var preservationPackaging = NewService("Preservation & Packaging", wedding, 300, 24, 18, 2, 0, 12);
        Price(preservationPackaging, "Lehenga", 300); Price(preservationPackaging, "Wedding Dress", 400);

        // ---- 10. Leather & Suede ----
        var leather = Cat("Leather & Suede");
        var leatherCleaning = NewService("Leather Cleaning", leather, 500, 72, 18, 1, 150, 48);
        Price(leatherCleaning, "Leather Jacket", 500); Price(leatherCleaning, "Leather Coat", 600); Price(leatherCleaning, "Handbag", 350);

        var suedeCleaning = NewService("Suede Cleaning", leather, 550, 72, 18, 2, 150, 48);
        Price(suedeCleaning, "Suede Jacket", 550); Price(suedeCleaning, "Boots", 400);

        // ---- 11. Alteration & Repair ----
        var alteration = Cat("Alteration & Repair");
        var basicAlteration = NewService("Basic Alteration", alteration, 100, 48, 5, 1, 0, 24);
        Price(basicAlteration, "Cotton Shirt", 100); Price(basicAlteration, "Cotton Pant", 120); Price(basicAlteration, "Blazer", 200);

        var zipperRepair = NewService("Zipper Repair", alteration, 150, 24, 5, 2, 0, 12);
        Price(zipperRepair, "Jeans", 150); Price(zipperRepair, "Blazer", 180);

        // ---- 12. Commercial / B2B Laundry ----
        var commercial = Cat("Commercial / B2B Laundry");
        var hotelLinen = NewService("Hotel Linen Wash", commercial, 40, 24, 12, 1, 15, 12);
        Price(hotelLinen, "Hotel Bedsheet", 40, PricingType.WeightBased); Price(hotelLinen, "Hotel Towel", 35, PricingType.WeightBased);

        var uniformWash = NewService("Corporate Uniform Wash", commercial, 80, 24, 12, 2, 20, 12);
        Price(uniformWash, "Corporate Uniform", 80); Price(uniformWash, "Chef Uniform", 90);

        // Garments are a shared pool priced under many services/categories — assign each
        // one a primary Category (its first-priced service's category) so the required
        // Garment.CategoryId has a sensible default; admins can reassign later.
        foreach (var price in allPrices)
        {
            var garment = price.Garment!;
            if (garment.Category is null)
                garment.Category = price.Service!.Category;
        }
        // Safety net: a garment with no Price() entry at all would otherwise get
        // inserted with CategoryId = Guid.Empty, violating the FK constraint and
        // crashing the whole seed on startup. Fall back to the first category.
        foreach (var garment in garments.Values)
        {
            if (garment.Category is null)
            {
                garment.Category = categories[0];
                logger.LogWarning("Garment '{Garment}' has no priced service; defaulted to category '{Category}'.", garment.Name, categories[0].Name);
            }
        }

        db.Services.AddRange(allServices);
        db.GarmentServicePrices.AddRange(allPrices);

        // ---- Add-ons ----
        db.AddOns.AddRange(
            new AddOn { Name = "Stain Removal", Price = 20 },
            new AddOn { Name = "Premium Packaging", Price = 15 },
            new AddOn { Name = "Express Processing", Price = 25 });

        await db.SaveChangesAsync();
        logger.LogInformation(
            "Seeded {Categories} categories, {Services} services, {Garments} garments, {Prices} priced combinations.",
            categories.Count, allServices.Count, garments.Count, allPrices.Count);
    }
}
