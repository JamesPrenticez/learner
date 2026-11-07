using System.Text.Json.Serialization;
using Learner.Api.Data;
using Learner.Api.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Read the connection string lazily so tests can override it.
builder.Services.AddDbContext<AppDb>((sp, o) =>
    o.UseSqlite(sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")));

builder.Services.AddIdentity<AppUser, IdentityRole>(o =>
{
    o.Password.RequiredLength = 6;
    o.Password.RequireDigit = o.Password.RequireLowercase = o.Password.RequireUppercase = o.Password.RequireNonAlphanumeric = false;
}).AddEntityFrameworkStores<AppDb>();

// Cookie auth for an API: answer 401/403 instead of redirecting to a login page.
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "learner.auth";
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<Access>();
builder.Services.Configure<CompetencyRules>(builder.Configuration.GetSection("Competency"));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Allowed origins live in appsettings.json ("Cors:AllowedOrigins").
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

await Seed.RunAsync(app.Services);

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuth();
app.MapBuilderCourses();
app.MapMedia();
app.MapLearning();
app.MapAssessment();
app.MapCompetencies();
app.MapExercises();

app.Run();

public partial class Program;
