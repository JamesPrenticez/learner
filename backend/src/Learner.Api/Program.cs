var builder = WebApplication.CreateBuilder(args);

// Allowed origins live in appsettings.json ("Cors:AllowedOrigins").
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => "Hello World!");

// Mock user, replace with a real lookup once auth exists.
app.MapGet("/api/user", () => new { Id = 1, Name = "Mock User", Email = "mock@example.com" });

app.Run();
