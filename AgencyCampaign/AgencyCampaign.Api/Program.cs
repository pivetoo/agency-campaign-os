using AgencyCampaign.Api.BackgroundJobs;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Infrastructure.DependencyInjection;
using Archon.Api.DependencyInjection;
using Archon.Api.MultiTenancy;
using Archon.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AgencyCampaignCors", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("public", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy("public-pdf", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});
builder.Services.AddArchonApi(builder.Configuration, typeof(AgencyCampaignResource));
builder.Services.AddAgencyCampaignInfrastructure(builder.Configuration);
builder.Services.AddServicesFromAssembly(typeof(Program).Assembly);
builder.Services.AddHostedService<SocialSyncJob>();
builder.Services.AddHostedService<ContentLicenseExpiryJob>();
builder.Services.AddHostedService<ProposalExpiryJob>();
builder.Services.AddArchonAuthentication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AgencyCampaignCors");
app.UseStaticFiles();

app.UseArchonApi();
app.UseAuthentication();
app.UseAuthorization();
app.UseSessionValidation();
app.UseRateLimiter();

app.MapControllers();

await app.UseArchonAccessSyncAsync();

app.Run();
