using System.Text;
using Calendar.Data;
using Microsoft.EntityFrameworkCore;
using Calendar.Repositories;
using Calendar.Interfaces;
using Calendar.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Calendar.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT config
builder.Services.AddSwaggerGen(c =>
{
    // 👇 This adds the required "openapi: 3.x" + version field
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Calendar API",
        Version = "v1",
        Description = "API for managing calendar appointments"
    });

    // 🔑 Add JWT Auth scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT token without Bearer prefix (e.g., 'eyJhb...')",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
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

// Read JWT settings from appsettings.json
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// MySQL DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Dependency Injection
// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
builder.Services.AddScoped<ITypeRepository, TypeRepository>();
builder.Services.AddScoped<IRecurrenceRuleRepository, RecurrenceRuleRepository>();

// Services
builder.Services.AddScoped<AppointmentService, AppointmentService>();
builder.Services.AddScoped<UserService, UserService>();
builder.Services.AddScoped<ParticipantService, ParticipantService>();

var app = builder.Build();

// SeedData(app);

void SeedData(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.Types.Any())
    {
        context.Types.AddRange(
            new Calendar.Models.Type { TypeName = "Meeting", ColorCode = "#FF5733" },
            new Calendar.Models.Type { TypeName = "Workshop", ColorCode = "#33FF57" },
            new Calendar.Models.Type { TypeName = "Conference", ColorCode = "#3357FF" }
        );

        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calendar API V1");
        c.RoutePrefix = "swagger"; // Swagger UI at /swagger
    });
}

app.UseHttpsRedirection();

// 🔑 Important: authentication before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();