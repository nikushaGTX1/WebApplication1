using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;


var builder = WebApplication.CreateBuilder(args);

// ------------------ SERVICES ------------------
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
// 🔥 USE POSTGRES (NOT SQLITE)
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

app.MapControllers();

app.MapGet("/", () => "API is running 🚀");

app.Run();
