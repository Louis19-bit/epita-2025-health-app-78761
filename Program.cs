using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient(); // for HttpClientFactory for external API calls
// Ollama API
builder.Services.AddSession(); // for session management and chat history


// 🟢 Configure SQLite Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// 🟢 Configure Identity (Users, Roles)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

// 🟢 Add Razor Pages & API Controllers
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Active le support API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hospital API", Version = "v1" });
});

var app = builder.Build();

// 🟢 Ensure Roles & Admin User are Created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateRolesAndAdminAsync(roleManager, userManager);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating roles: {ex.Message}");
    }
}

// 🟢 Middleware Configuration
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hospital API v1");
});

app.MapRazorPages();
app.MapControllers(); // 🟢 Active les API

app.Run();

/// <summary>
/// Creates roles (Admin, Doctor, Patient) and an initial admin user.
/// </summary>
async Task CreateRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
{
    string[] roleNames = { "Admin", "Doctor", "Patient" };
    foreach (var role in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // 🟢 Create an Admin User if not exists
    string adminEmail = "admin@hospital.com";
    string adminPassword = "Admin@123";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Hospital Admin",
            Role = "Admin"
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}