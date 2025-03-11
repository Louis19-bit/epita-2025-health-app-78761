using Microsoft.EntityFrameworkCore;
using HospitalAppointmentSystem.Data;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Configuration de la base de données SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Ajout de l'authentification et gestion des utilisateurs
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Middleware d'authentification et autorisation
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Active les pages de connexion d'Identity
app.MapRazorPages();

/*
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
*/

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Appointment}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "admin",
    pattern: "{controller=Admin}/{action=Doctors}/{id?}");

app.MapControllerRoute(
    name: "doctor",
    pattern: "{controller=Doctor}/{action=Appointments}/{id?}");

app.MapControllerRoute(
    name: "appointment",
    pattern: "{controller=Appointment}/{action=Index}/{id?}");

app.Run();

