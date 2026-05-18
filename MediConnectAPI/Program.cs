using System.Text;
using MediConnectAPI.Data;
using MediConnectAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Database
// Connects to SQL Server using the conection string in appsettings.json
builder.Services.AddDbContext<MediConnectDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
// Reads JWT settings from the appsettings.json
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        // Sets JWT Bearer as 'default' which overrides cookie defaults
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero // No grace period making tokens expire exactly on time
        };
    });

builder.Services.AddAuthorization();

// Application Services 
// TokenService: scoped = one instance per HTTP request
builder.Services.AddScoped<ITokenService, TokenService>();

// CORS 
// During deveopment: allows all origins so the MVC app and reporting app can call this API without CORS errors.
// Before deploymnt: replace AllowAnyOrigin with the specific Azure URLs.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Controllers/Swagger 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MediConnect API",
        Version = "v1",
        Description = "Healthcare Clinic Appointment & Resource System — IT8118 Group Project"
    });

    // This adds the green "Authorize" button to Swagger UI.
    // After loging in via POST /api/auth/login, paste the token here and all subsequent requests will include Authorization: Bearer {token}

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT token from POST /api/auth/login"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Build 
var app = builder.Build();

// The Middleware Pipeline 
// Order matters, UseAuthentication has to come before UseAuthorization
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MediConnect API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication(); // Reads the JWT from Authorization header and sets the HttpContext.User
app.UseAuthorization();  // Evaluates [Authorize] attributes against the HttpContext.User

app.MapControllers();

app.Run();