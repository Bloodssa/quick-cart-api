using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuickCartApi.Common;
using QuickCartApi.Data;
using QuickCartApi.Extensions;
using QuickCartApi.Options;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddOptions<ShippingOptions>()
    .BindConfiguration(ShippingOptions.SectionName)
    .Validate(o => o.FreeShippingThreshold > 0 && o.MaxShippingFee > 0, "Shipping: threshold and cap must be above 0.")
    .Validate(o => o.Rates.Count > 0 && o.Rates.Values.All(r => r >= 0), "Shipping: rates must be set and not negative.")
    .Validate(o => o.Tiers.All(t => t.Share > 0 && t.Percent is >= 0 and <= 100), "Shipping: each tier needs a share above 0 and a percent from 0 to 100.")
    .ValidateOnStart();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://quick-cart.wmsys.online"
            ).AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Retry-After");
    });
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateIssuerSigningKey = true
        };

        // send an not auth message
        options.Events = new JwtBearerEvents
        {
            // not auth message response
            OnChallenge = context =>
            {
                // suppress the default empty 401 response
                context.HandleResponse();

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var payload = System.Text.Json.JsonSerializer.Serialize(new { message = "You are not authenticated" });
                return context.Response.WriteAsync(payload);
            },

            // Forbidden routes
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var payload = System.Text.Json.JsonSerializer.Serialize(new { message = "You are not allowed to access this resources"} );
                return context.Response.WriteAsync(payload);
            }
        };
    });

builder.Services.AddApplicationServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddApiVersioningSetup();
builder.Services.AddRateLimitingSetup();
builder.Services.AddOpenApi("v1");
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.AddDocument("v1", "QuickCart API v1", "/openapi/v1.json");
});


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();
