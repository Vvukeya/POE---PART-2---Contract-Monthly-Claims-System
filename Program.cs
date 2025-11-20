using POE___PART_2___Contract_Monthly_Claims_System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Add session middleware
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// HR routes
app.MapControllerRoute(
    name: "hr",
    pattern: "HR/{action=HRDashboard}",
    defaults: new { controller = "HR" });

// Initialize database on startup
try
{
    var dbConnection = new DatabaseConnection();
    Console.WriteLine("Application started successfully with database initialization.");
}
catch (Exception ex)
{
    Console.WriteLine($"Database initialization note: {ex.Message}");
}

app.Run();