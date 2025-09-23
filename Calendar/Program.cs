using Calendar.Business;
using Calendar.Data;
using Calendar.DataAccess;
using Calendar.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DotNetEnv;

// Load .env file at the very beginning
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration (this will include .env variables)
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Calendar API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database Configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Configuration - Now it will use JWT_SECRET_KEY from .env if available
var jwtSecretKey = builder.Configuration["JWT_SECRET_KEY"] ?? builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtSecretKey))
{
    throw new InvalidOperationException("JWT SecretKey not configured");
}

var key = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Set to true in production
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

if (builder.Environment.IsDevelopment())
{
    // In development, use Scoped to pick up .env changes without restart
    builder.Services.AddScoped<JwtHelper>();
    builder.Services.AddScoped<PasswordHasher>();
}
else
{
    // In production, use Singleton for performance
    builder.Services.AddSingleton<JwtHelper>();
    builder.Services.AddSingleton<PasswordHasher>();
}

// Scoped pros
//     DbContext Compatibility - DbContext is scoped, DAL can safely use it
//     No Thread Safety Concerns - Each request gets its own instance
//     Transaction Support - Can maintain transactions within request scope
//     Memory Management - Disposed after each request
//     State Isolation - No risk of data bleeding between requests

// Singleton cons
//     Cannot Use DbContext - DbContext is scoped, causes runtime error
//     Thread Safety Required - Complex concurrent access handling
//     Connection Management - Must manage DB connections manually
//     Memory Leaks Risk - Data accumulation over time

// Dependency Injection
builder.Services.AddScoped<IAuthBL, AuthBL>();
builder.Services.AddScoped<IAppointmentBL, AppointmentBL>();
builder.Services.AddScoped<IAppointmentAttendeeBL, AppointmentAttendeeBL>();

builder.Services.AddScoped<IAppointmentDAL, AppointmentDAL>();
builder.Services.AddScoped<IRecurrenceRuleDAL, RecurrenceRuleDAL>();
builder.Services.AddScoped<IUserDAL, UserDAL>();
builder.Services.AddScoped<IAppointmentAttendeeDAL, AppointmentAttendeeDAL>();
builder.Services.AddScoped<IAppointmentTypeDAL, AppointmentTypeDAL>();


// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calendar API V1"); });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();