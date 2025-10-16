using IceIceBaby.Models;
using Microsoft.EntityFrameworkCore;

namespace IceIceBaby.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        // Seed or update Products with prices
        var seedProducts = new List<Product>
        {
            new Product { Sku = "ICE-2KG", Name = "Ice 2 kg Bag", UnitPrice = 15.00m },
            new Product { Sku = "ICE-4KG", Name = "Ice 4 kg Bag", UnitPrice = 25.00m },
            new Product { Sku = "ICE-10KG", Name = "Ice 10 kg Bag", UnitPrice = 60.00m }
        };

        foreach (var p in seedProducts)
        {
            var existing = await db.Products.FirstOrDefaultAsync(x => x.Sku == p.Sku, ct);
            if (existing == null)
            {
                db.Products.Add(p);
            }
            else
            {
                // Update name/price if changed
                if (existing.Name != p.Name) existing.Name = p.Name;
                if (existing.UnitPrice != p.UnitPrice) existing.UnitPrice = p.UnitPrice;
            }
        }

        // Seed Customers
        var seedCustomers = new List<Customer>
        {
            new Customer { Name = "Ocean View Hotel", Phone = "+264 81 000 0001", Email = "orders@oceanview.example", Address = "1 Beach Rd" },
            new Customer { Name = "Sunset Bar & Grill", Phone = "+264 81 000 0002", Email = "manager@sunsetbar.example", Address = "22 Palm Ave" },
            new Customer { Name = "City Supermarket", Phone = "+264 81 000 0003", Email = "procurement@citysuper.example", Address = "100 Main St" },
            new Customer { Name = "Harbor Seafood", Phone = "+264 81 000 0004", Email = "orders@harborseafood.example", Address = "9 Dock Lane" },
            new Customer { Name = "Event Co.", Phone = "+264 81 000 0005", Email = "events@eventco.example", Address = "45 Expo Blvd" }
        };

        foreach (var c in seedCustomers)
        {
            var exists = await db.Customers.AnyAsync(x => x.Name == c.Name, ct);
            if (!exists) db.Customers.Add(c);
        }

        // Seed Drivers
        var seedDrivers = new List<Driver>
        {
            new Driver { Name = "John Driver", Phone = "+264 81 200 1111" },
            new Driver { Name = "Mary Wheels", Phone = "+264 81 200 2222" }
        };

        foreach (var d in seedDrivers)
        {
            var exists = await db.Drivers.AnyAsync(x => x.Name == d.Name, ct);
            if (!exists) db.Drivers.Add(d);
        }

        // Seed Vehicles
        var seedVehicles = new List<Vehicle>
        {
            new Vehicle { Name = "Truck 1", Plate = "N 123-456 W" },
            new Vehicle { Name = "Truck 2", Plate = "N 987-654 W" }
        };

        foreach (var v in seedVehicles)
        {
            var exists = await db.Vehicles.AnyAsync(x => x.Name == v.Name, ct);
            if (!exists) db.Vehicles.Add(v);
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
