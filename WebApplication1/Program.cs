using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

var builder = WebApplication.CreateBuilder(args);

// ------------------ SERVICES ------------------

// Add controllers
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ------------------ DATABASE CONFIG ------------------

// 🔥 Use PostgreSQL (Aiven)
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 🔥 Required for PostgreSQL timestamp handling
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var app = builder.Build();

// ------------------ APPLY DB MIGRATIONS AUTOMATICALLY ------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();

    // ✅ DO NOT delete the database on Aiven
    // db.Database.EnsureDeleted(); // ❌ REMOVE THIS

    // Only apply pending migrations safely
    db.Database.Migrate();
}

// ------------------ PIPELINE ------------------

// Enable Swagger ALWAYS (Render = Production)
app.UseSwagger();
app.UseSwaggerUI();

// Serve static files (uploads)
app.UseStaticFiles();

app.UseCors("AllowAngularDev");

app.UseAuthorization();

// Map controllers
app.MapControllers();

// Root endpoint
app.MapGet("/", () => "API is running 🚀");

app.Run();
