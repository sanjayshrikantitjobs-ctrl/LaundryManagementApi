using LaundryMgmt.API.Hubs;
using LaundryMgmt.API.Middleware;
using LaundryMgmt.Application;
using LaundryMgmt.Infrastructure;
using LaundryMgmt.Infrastructure.Identity;
using LaundryMgmt.Infrastructure.Persistence;
using MediatR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging ----
builder.Host.UseSerilog((context, cfg) => cfg.ReadFrom.Configuration(context.Configuration));

// ---- Layers ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// MediatR only scans the Application assembly by default (see
// LaundryMgmt.Application.DependencyInjection). Also scan this assembly so
// API-layer notification handlers, like OrderStatusChangedSignalRHandler
// (needs IHubContext<OrderStatusHub>, an API-layer type), get registered too.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// ---- Web / API plumbing ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

const string AngularCorsPolicy = "AngularClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // required for SignalR
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Seeds an Admin and a Customer login so there's something to sign in with out of
    // the box. Dev-only: seed real users through a proper Register/invite flow elsewhere.
    await IdentitySeeder.SeedAsync(app.Services);
    await CatalogSeeder.SeedAsync(app.Services);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles(); // serves wwwroot/uploads/* (see UploadsController) at /uploads/*
app.UseCors(AngularCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderStatusHub>("/hubs/order-status");

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
