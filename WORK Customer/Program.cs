using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

// Visar sökvägen till SQLite-databasen (för felsökning) och initierar DbContext.
Console.WriteLine("DB: " + Path.Combine(AppContext.BaseDirectory, "ecommerce.db"));

using var db = new WORK_Customer.ECommerceContext();
// Ser till att databasen finns — skapar den automatiskt om den saknas. Praktiskt under utveckling.
await db.Database.EnsureCreatedAsync();

// Seed: lägger till exempeldata i kategorier och produkter om tabellerna är tomma.
if (!await db.Categories.AnyAsync())
{
    db.Categories.AddRange(
        new WORK_Customer.Category { Name = "Books" },
        new WORK_Customer.Category { Name = "Movies" }
    );
    await db.SaveChangesAsync();
    Console.WriteLine("Seeded categories");
}

if (!await db.Products.AnyAsync())
{
    db.Products.AddRange(
        new WORK_Customer.Product { name = "Hammer", price = 249, CategoryID = 1 },
        new WORK_Customer.Product { name = "Shirt", price = 130, CategoryID = 1 }
    );
    await db.SaveChangesAsync();
    Console.WriteLine("Seeded products");
}

// Enkel kommandoradsmeny. Välj vilken del av appen du vill arbeta med.
while (true)
{
    Console.WriteLine();
    Console.WriteLine("Main: customers | products | categories | orders | exit");
    var cmd = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
    if (cmd == "exit") break;

    switch (cmd)
    {
        case "customers":
            await CustomersMenu();
            break;
        case "products":
            await ProductsMenu();
            break;
        case "categories":
            await CategoriesMenu();
            break;
        case "orders":
            await OrdersMenu();
            break;
        default:
            Console.WriteLine("Unknown");
            break;
    }
}


// ----- Kundhantering -----
// Kundmeny: listar, lägger till, redigerar och tar bort kunder.
async Task CustomersMenu()
{
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("Customers: list | add | edit | delete | back");
        var c = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
        if (c == "back") break;
        switch (c)
        {
            case "list": await ListCustomers(); break;
            case "add": await AddCustomer(); break;
            case "edit": await EditCustomer(); break;
            case "delete": await DeleteCustomer(); break;
            default: Console.WriteLine("Unknown"); break;
        }
    }
}

// Hämtar alla kunder utan tracking (AsNoTracking) för snabbare läsning och skriver ut dem.
async Task ListCustomers()
{
    using var ctx = new WORK_Customer.ECommerceContext();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var rows = await ctx.Customers.AsNoTracking().OrderBy(c => c.CustomerId).ToListAsync();
    sw.Stop();
    foreach (var r in rows) Console.WriteLine($"{r.CustomerId} | {r.CustomerName} {r.LastName} | {r.Email}");
    // Visar hur lång tid frågan tog
    Console.WriteLine($"Query time: {sw.ElapsedMilliseconds} ms");
}

// Hjälpmetoder för lösenordshashning (PBKDF2/Rfc2898)
string HashPassword(string password)
{
    const int iterations = 100_000;
    var salt = RandomNumberGenerator.GetBytes(16);
    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
    var hash = pbkdf2.GetBytes(32);
    return $"{iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
}

// Verifierar ett lösenord mot den sparade salt+hash-strängen i konstant tid.
bool VerifyPassword(string password, string storedHash)
{
    try
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 3) return false;
        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var hash = Convert.FromBase64String(parts[2]);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var computed = pbkdf2.GetBytes(hash.Length);
        return CryptographicOperations.FixedTimeEquals(computed, hash);
    }
    catch
    {
        return false;
    }
}

// Lägger till en ny kund efter enklare validering av inmatade värden.
async Task AddCustomer()
{
    Console.Write("First name: ");
    var first = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("Last name: ");
    var last = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("Email: ");
    var email = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("Password: ");
    var password = (Console.ReadLine() ?? string.Empty).Trim();

    // Grundläggande validering
    if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        Console.WriteLine("All fields are required.");
        return;
    }
    if (!email.Contains("@") || email.Length > 200) { Console.WriteLine("Invalid email"); return; }
    if (first.Length > 100 || last.Length > 100) { Console.WriteLine("Name too long"); return; }

    using var ctx = new WORK_Customer.ECommerceContext();
    var c = new WORK_Customer.Customer
    {
        CustomerName = first,
        LastName = last,
        Email = email,
        // Spara lösenordet som en salt+hash-sträng
        PasswordHash = HashPassword(password)
    };
    ctx.Customers.Add(c);
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Customer added."); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// Redigera en befintlig kund: mata in nya värden eller lämna blankt för att behålla.
async Task EditCustomer()
{
    Console.Write("CustomerId to edit: ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var id)) { Console.WriteLine("Bad id"); return; }
    using var ctx = new WORK_Customer.ECommerceContext();
    var c = await ctx.Customers.FindAsync(id);
    if (c == null) { Console.WriteLine("Not found"); return; }
    Console.WriteLine($"Editing {c.CustomerName} {c.LastName} ({c.Email})");
    Console.Write("New first (blank to keep): "); var first = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("New last (blank to keep): "); var last = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("New email (blank to keep): "); var email = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("New password (blank to keep): "); var password = (Console.ReadLine() ?? string.Empty).Trim();
    if (!string.IsNullOrEmpty(first)) c.CustomerName = first;
    if (!string.IsNullOrEmpty(last)) c.LastName = last;
    if (!string.IsNullOrEmpty(email)) { if (!email.Contains("@")) { Console.WriteLine("Invalid email"); return; } c.Email = email; }
    if (!string.IsNullOrEmpty(password)) c.PasswordHash = HashPassword(password);
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Updated"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// Tar bort kund efter bekräftelse.
async Task DeleteCustomer()
{
    Console.Write("CustomerId to delete: ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var id)) { Console.WriteLine("Bad id"); return; }
    using var ctx = new WORK_Customer.ECommerceContext();
    var c = await ctx.Customers.FindAsync(id);
    if (c == null) { Console.WriteLine("Not found"); return; }
    Console.Write($"Confirm delete {c.CustomerName} {c.LastName} (y/N): ");
    var ok = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
    if (ok != "y") { Console.WriteLine("Aborted"); return; }
    ctx.Customers.Remove(c);
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Deleted"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// ----- Kategorier -----
async Task CategoriesMenu()
{
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("Categories: list | add | edit | delete | back");
        var c = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
        if (c == "back") break;
        switch (c)
        {
            case "list": await ListCategories(); break;
            case "add": await AddCategory(); break;
            case "edit": await EditCategory(); break;
            case "delete": await DeleteCategory(); break;
            default: Console.WriteLine("Unknown"); break;
        }
    }
}

// Redigera en kategori
async Task EditCategory()
{
    Console.Write("CategoryId to edit: ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var id)) { Console.WriteLine("Bad id"); return; }
    using var ctx = new WORK_Customer.ECommerceContext();
    var c = await ctx.Categories.FindAsync(id);
    if (c == null) { Console.WriteLine("Not found"); return; }
    Console.WriteLine($"Editing {c.Name}");
    Console.Write("New name (blank to keep): "); var name = (Console.ReadLine() ?? string.Empty).Trim();
    if (!string.IsNullOrEmpty(name)) c.Name = name;
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Updated"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// Tar bort en kategori efter bekräftelse
async Task DeleteCategory()
{
    Console.Write("CategoryId to delete: ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var id)) { Console.WriteLine("Bad id"); return; }
    using var ctx = new WORK_Customer.ECommerceContext();
    var c = await ctx.Categories.FindAsync(id);
    if (c == null) { Console.WriteLine("Not found"); return; }
    Console.Write($"Confirm delete category {c.Name} (y/N): ");
    var ok = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
    if (ok != "y") { Console.WriteLine("Aborted"); return; }
    ctx.Categories.Remove(c);
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Deleted"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// ----- Produkter -----
async Task ProductsMenu()
{
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("Products: list | add | edit | delete | back");
        var c = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
        if (c == "back") break;
        switch (c)
        {
            case "list": await ListProducts(); break;
            case "add": await AddProduct(); break;
            case "edit": await EditProduct(); break;
            case "delete": await DeleteProduct(); break;
            default: Console.WriteLine("Unknown"); break;
        }
    }
}

// Redigera en produkt
async Task EditProduct()
{
    Console.Write("Product id to edit: ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var id)) { Console.WriteLine("Bad id"); return; }
    using var ctx = new WORK_Customer.ECommerceContext();
    var p = await ctx.Products.FindAsync(id);
    if (p == null) { Console.WriteLine("Not found"); return; }
    Console.WriteLine($"Editing {p.name} | {p.price}");
    Console.Write("New name (blank to keep): "); var name = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("New price (blank to keep): "); var pr = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("New category id (blank to keep): "); var cat = (Console.ReadLine() ?? string.Empty).Trim();
    if (!string.IsNullOrEmpty(name)) p.name = name;
    if (!string.IsNullOrEmpty(pr) && decimal.TryParse(pr, out var price) && price >= 0) p.price = price;
    if (!string.IsNullOrEmpty(cat) && int.TryParse(cat, out var cid)) p.CategoryID = cid;
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Updated"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// Tar bort en produkt efter bekräftelse
async Task DeleteProduct()
{
    Console.Write("Product id to delete: ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var id)) { Console.WriteLine("Bad id"); return; }
    using var ctx = new WORK_Customer.ECommerceContext();
    var p = await ctx.Products.FindAsync(id);
    if (p == null) { Console.WriteLine("Not found"); return; }
    Console.Write($"Confirm delete product {p.name} (y/N): ");
    var ok = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
    if (ok != "y") { Console.WriteLine("Aborted"); return; }
    ctx.Products.Remove(p);
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Deleted"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// ----- Ordrar -----
async Task OrdersMenu()
{
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("Orders: list | add | view | clear | back");
        var c = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
        if (c == "back") break;
        switch (c)
        {
            case "list": await ListOrders(); break;
            case "add": await AddOrder(); break;
            case "view": await ViewOrder(); break;
            case "clear": await ClearOrders(); break;
            default: Console.WriteLine("Unknown"); break;
        }
    }
}

// Lista produkter och mät frågetiden
async Task ListProducts() // overload used by menus
{
    using var ctx = new WORK_Customer.ECommerceContext();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var rows = await ctx.Products.AsNoTracking().OrderBy(p => p.id).ToListAsync();
    sw.Stop();
    foreach (var r in rows) Console.WriteLine($"{r.id} | {r.name} | {r.price} | Category {r.CategoryID}");
    Console.WriteLine($"Query time: {sw.ElapsedMilliseconds} ms");
}

// Lista ordrar och inkludera kundinformation
async Task ListOrders()
{
    using var ctx = new WORK_Customer.ECommerceContext();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var rows = await ctx.Orders.Include(o => o.Customer).AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync();
    sw.Stop();
    foreach (var r in rows) Console.WriteLine($"{r.OrderId} | {r.CreatedAt} | Customer {r.Customer.CustomerName} {r.Customer.LastName}");
    Console.WriteLine($"Query time: {sw.ElapsedMilliseconds} ms");
}

// Visa en order inklusive rader och produktinformation
async Task ViewOrder()
{
    Console.Write("OrderId to view: ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var id)) { Console.WriteLine("Bad id"); return; }
    using var ctx = new WORK_Customer.ECommerceContext();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var order = await ctx.Orders.Include(o => o.Rows).ThenInclude(r => r.Product).Include(o => o.Customer).AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == id);
    sw.Stop();
    if (order == null) { Console.WriteLine("Not found"); return; }
    Console.WriteLine($"Order {order.OrderId} - {order.CreatedAt} - Customer: {order.Customer.CustomerName} {order.Customer.LastName}");
    foreach (var row in order.Rows)
    {
        Console.WriteLine($"  {row.OrderRowId} | Product: {row.Product.name} | Qty: {row.Quantity} | Unit: {row.UnitPrice} | Sub: {row.Quantity * row.UnitPrice}");
    }
    Console.WriteLine($"Query time: {sw.ElapsedMilliseconds} ms");
}

// Skapa order interaktivt genom att lägga till rader; spara i transaktion för att säkerställa konsistens
async Task AddOrder()
{
    Console.Write("CustomerId for order: ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var custId)) { Console.WriteLine("Bad id"); return; }

    using var ctx = new WORK_Customer.ECommerceContext();
    var customer = await ctx.Customers.FindAsync(custId);
    if (customer == null) { Console.WriteLine("Customer not found"); return; }

    var order = new WORK_Customer.Order { CustomerId = custId, CreatedAt = DateTime.UtcNow };

    // Samla orderrader från användaren
    while (true)
    {
        Console.WriteLine("Available products:");
        var prods = await ctx.Products.AsNoTracking().OrderBy(p => p.id).ToListAsync();
        foreach (var p in prods) Console.WriteLine($"{p.id} | {p.name} | {p.price}");

        Console.Write("Add product id (blank to finish, or type 'list' to show products): ");
        var prodIn = (Console.ReadLine() ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(prodIn)) break;
        if (prodIn.Equals("list", StringComparison.OrdinalIgnoreCase)) continue;
        if (!int.TryParse(prodIn, out var pid)) { Console.WriteLine("Bad id"); continue; }

        // Hämta produkt utan tracking för att undvika konflikter med kontext
        var product = await ctx.Products.AsNoTracking().FirstOrDefaultAsync(x => x.id == pid);
        if (product == null) { Console.WriteLine("Product not found"); continue; }
        Console.WriteLine($"Selected product: {product.id} | {product.name} | {product.price}");
        Console.Write("Quantity: ");
        if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var qty) || qty <= 0) { Console.WriteLine("Bad qty"); continue; }

        var row = new WORK_Customer.OrderRow { ProductId = pid, Quantity = qty, UnitPrice = product.price };
        order.Rows.Add(row);
        Console.WriteLine("Added row");
    }

    if (!order.Rows.Any()) { Console.WriteLine("No rows added, aborting"); return; }

    // Spara ordren i en databas-transaktion
    using var transaction = await ctx.Database.BeginTransactionAsync();
    try
    {
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();
        await transaction.CommitAsync();
        Console.WriteLine($"Order {order.OrderId} created.");
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.WriteLine("Error saving order: " + ex.GetBaseException().Message);
    }
}

// Rensa alla ordrar och orderrader (farlig operation, använd endast för test)
async Task ClearOrders()
{
    using var ctx = new WORK_Customer.ECommerceContext();
    using var tx = await ctx.Database.BeginTransactionAsync();
    try
    {
        // Ta bort orderrader först, sedan ordrar
        var rows = ctx.OrderRows;
        ctx.OrderRows.RemoveRange(rows);
        await ctx.SaveChangesAsync();

        var orders = ctx.Orders;
        ctx.Orders.RemoveRange(orders);
        await ctx.SaveChangesAsync();

        await tx.CommitAsync();
        Console.WriteLine("All orders cleared.");
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        Console.WriteLine("Error clearing orders: " + ex.GetBaseException().Message);
    }
}

// Hjälpfunktioner för kategorier och produkter
async Task ListCategories()
{
    using var ctx = new WORK_Customer.ECommerceContext();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var rows = await ctx.Categories.AsNoTracking().OrderBy(c => c.CategoryId).ToListAsync();
    sw.Stop();
    foreach (var r in rows) Console.WriteLine($"{r.CategoryId} | {r.Name}");
    Console.WriteLine($"Query time: {sw.ElapsedMilliseconds} ms");
}

async Task AddCategory()
{
    Console.Write("Name: ");
    var name = (Console.ReadLine() ?? string.Empty).Trim();
    if (string.IsNullOrEmpty(name) || name.Length > 200)
    {
        Console.WriteLine("Invalid");
        return;
    }
    using var ctx = new WORK_Customer.ECommerceContext();
    ctx.Categories.Add(new WORK_Customer.Category { Name = name });
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Added"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message);}        
}

async Task AddProduct()
{
    Console.Write("Name: ");
    var name = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("Price: ");
    var p = (Console.ReadLine() ?? string.Empty).Trim();
    if(!decimal.TryParse(p,out var price) || price < 0) { Console.WriteLine("Bad price"); return; }
    Console.Write("CategoryId: ");
    if(!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var cid)) { Console.WriteLine("Bad category id"); return; }
    using var ctx = new WORK_Customer.ECommerceContext();
    ctx.Products.Add(new WORK_Customer.Product { name = name, price = price, CategoryID = cid });
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Added product"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}
