using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WORK_Customer.Security;

// Visar s�kv�gen till SQLite-databasen (f�r fels�kning) och initierar DbContext.
Console.WriteLine("DB: " + Path.Combine(AppContext.BaseDirectory, "ecommerce.db"));

using var db = new WORK_Customer.ECommerceContext();
// Anv�nder EF Core-migrationer som den gemensamma k�llan f�r databasens schema.
await db.Database.MigrateAsync();

// Seed: l�gger till exempeldata i kategorier och produkter om tabellerna �r tomma.
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

// Enkel kommandoradsmeny. V�lj vilken del av appen du vill arbeta med.
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
// Kundmeny: listar, l�gger till, redigerar och tar bort kunder.
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

// H�mtar alla kunder utan tracking (AsNoTracking) f�r snabbare l�sning och skriver ut dem.
async Task ListCustomers()
{
    using var ctx = new WORK_Customer.ECommerceContext();
    Console.Write("Search name or email (blank for all): ");
    var search = (Console.ReadLine() ?? string.Empty).Trim();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var query = ctx.Customers.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(c => c.CustomerName.Contains(search) || c.LastName.Contains(search) || c.Email.Contains(search));
    var rows = await query.OrderBy(c => c.CustomerId).ToListAsync();
    sw.Stop();
    foreach (var r in rows) Console.WriteLine($"{r.CustomerId} | {r.CustomerName} {r.LastName} | {r.Email}");
    // Visar hur l�ng tid fr�gan tog
    Console.WriteLine($"Query time: {sw.ElapsedMilliseconds} ms");
}

// Hj�lpmetoder f�r l�senordshashning (PBKDF2/Rfc2898)

// L�gger till en ny kund efter enklare validering av inmatade v�rden.
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

    // Grundl�ggande validering
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
        // Spara l�senordet som en salt+hash-str�ng
        PasswordHash = PasswordHasher.Hash(password)
    };
    ctx.Customers.Add(c);
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Customer added."); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// Redigera en befintlig kund: mata in nya v�rden eller l�mna blankt f�r att beh�lla.
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
    if (!string.IsNullOrEmpty(first)) { if (first.Length > 100) { Console.WriteLine("Name too long"); return; } c.CustomerName = first; }
    if (!string.IsNullOrEmpty(last)) { if (last.Length > 100) { Console.WriteLine("Name too long"); return; } c.LastName = last; }
    if (!string.IsNullOrEmpty(email)) { if (!email.Contains("@") || email.Length > 200) { Console.WriteLine("Invalid email"); return; } c.Email = email; }
    if (!string.IsNullOrEmpty(password)) c.PasswordHash = PasswordHasher.Hash(password);
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Updated"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// Tar bort kund efter bekr�ftelse.
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
    if (!string.IsNullOrEmpty(name))
    {
        if (name.Length > 200) { Console.WriteLine("Name too long"); return; }
        c.Name = name;
    }
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Updated"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// Tar bort en kategori efter bekr�ftelse
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

async Task EditProduct()
{
    Console.Write("Product id to edit: ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var id)) { Console.WriteLine("Bad id"); return; }
    using var ctx = new WORK_Customer.ECommerceContext();
    var product = await ctx.Products.FindAsync(id);
    if (product == null) { Console.WriteLine("Not found"); return; }
    Console.WriteLine($"Editing {product.name} | {product.price}");
    Console.Write("New name (blank to keep): "); var name = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("New price (blank to keep): "); var priceInput = (Console.ReadLine() ?? string.Empty).Trim();
    Console.Write("New category id (blank to keep): "); var categoryInput = (Console.ReadLine() ?? string.Empty).Trim();
    if (!string.IsNullOrEmpty(name))
    {
        if (name.Length > 200) { Console.WriteLine("Name too long"); return; }
        product.name = name;
    }
    if (!string.IsNullOrEmpty(priceInput))
    {
        if (!decimal.TryParse(priceInput, out var price) || price < 0) { Console.WriteLine("Bad price"); return; }
        product.price = price;
    }
    if (!string.IsNullOrEmpty(categoryInput))
    {
        if (!int.TryParse(categoryInput, out var categoryId)) { Console.WriteLine("Bad category id"); return; }
        if (!await ctx.Categories.AnyAsync(c => c.CategoryId == categoryId)) { Console.WriteLine("Category not found"); return; }
        product.CategoryID = categoryId;
    }
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Updated"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}

// Tar bort en produkt efter bekr�ftelse
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

// Lista produkter och m�t fr�getiden
async Task ListProducts() // overload used by menus
{
    using var ctx = new WORK_Customer.ECommerceContext();
    Console.Write("Search product name (blank for all): ");
    var search = (Console.ReadLine() ?? string.Empty).Trim();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var query = ctx.Products.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.name.Contains(search));
    var rows = await query.OrderBy(p => p.id).ToListAsync();
    sw.Stop();
    foreach (var r in rows) Console.WriteLine($"{r.id} | {r.name} | {r.price} | Category {r.CategoryID}");
    Console.WriteLine($"Query time: {sw.ElapsedMilliseconds} ms");
}

// Lista ordrar och inkludera kundinformation
async Task ListOrders()
{
    using var ctx = new WORK_Customer.ECommerceContext();
    Console.Write("Search customer name (blank for all): ");
    var search = (Console.ReadLine() ?? string.Empty).Trim();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var query = ctx.Orders.Include(o => o.Customer).AsNoTracking();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(o => o.Customer.CustomerName.Contains(search) || o.Customer.LastName.Contains(search));
    var rows = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
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

// Skapa order interaktivt genom att l�gga till rader; spara i transaktion f�r att s�kerst�lla konsistens
async Task AddOrder()
{
    Console.Write("CustomerId for order: ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var custId)) { Console.WriteLine("Bad id"); return; }

    using var ctx = new WORK_Customer.ECommerceContext();
    var customer = await ctx.Customers.FindAsync(custId);
    if (customer == null) { Console.WriteLine("Customer not found"); return; }

    var order = new WORK_Customer.Order { CustomerId = custId, CreatedAt = DateTime.UtcNow };

    // Samla orderrader fr�n anv�ndaren
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

        // H�mta produkt utan tracking f�r att undvika konflikter med kontext
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

// Rensa alla ordrar och orderrader (farlig operation, anv�nd endast f�r test)
async Task ClearOrders()
{
    using var ctx = new WORK_Customer.ECommerceContext();
    using var tx = await ctx.Database.BeginTransactionAsync();
    try
    {
        // Ta bort orderrader f�rst, sedan ordrar
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

// Hj�lpfunktioner f�r kategorier och produkter
async Task ListCategories()
{
    using var ctx = new WORK_Customer.ECommerceContext();
    Console.Write("Search category name (blank for all): ");
    var search = (Console.ReadLine() ?? string.Empty).Trim();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var query = ctx.Categories.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.Name.Contains(search));
    var rows = await query.OrderBy(c => c.CategoryId).ToListAsync();
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
    if (string.IsNullOrWhiteSpace(name) || name.Length > 200) { Console.WriteLine("Invalid product name"); return; }
    Console.Write("Price: ");
    var p = (Console.ReadLine() ?? string.Empty).Trim();
    if(!decimal.TryParse(p,out var price) || price < 0) { Console.WriteLine("Bad price"); return; }
    Console.Write("CategoryId: ");
    if(!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out var cid)) { Console.WriteLine("Bad category id"); return; }
    using var ctx = new WORK_Customer.ECommerceContext();
    if (!await ctx.Categories.AnyAsync(c => c.CategoryId == cid)) { Console.WriteLine("Category not found"); return; }
    ctx.Products.Add(new WORK_Customer.Product { name = name, price = price, CategoryID = cid });
    try { await ctx.SaveChangesAsync(); Console.WriteLine("Added product"); }
    catch (DbUpdateException ex) { Console.WriteLine("DB error: " + ex.GetBaseException().Message); }
}
