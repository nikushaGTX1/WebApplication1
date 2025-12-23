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
        policy.WithOrigins(
            "http://localhost:4200",
            "https://your-angular-domain-if-any.com"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ------------------ DATABASE CONFIG ------------------
// USE SQLITE (Works on Render Linux)
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ------------------ BUILD APP ------------------
var app = builder.Build();

// Swagger only in dev OR enable always if you want
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirect (Render supports it)
app.UseHttpsRedirection();

app.UseCors("AllowAngularDev");

app.UseAuthorization();

app.MapControllers();

// Health check root
app.MapGet("/", () => "API is running 🚀");

// Run app
app.Run();
