using AgencyCampaign.Api.Hubs;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.DependencyInjection;
using Archon.Api.DependencyInjection;
using Archon.Api.MultiTenancy;
using Archon.Infrastructure.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
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
builder.Services.AddArchonApi(builder.Configuration, typeof(AgencyCampaignResource));
builder.Services.AddAgencyCampaignInfrastructure(builder.Configuration);
builder.Services.AddServicesFromAssembly(typeof(Program).Assembly);
builder.Services.AddArchonAuthentication(builder.Configuration);
builder.Services.AddScoped<IWhatsAppNotifier, SignalRWhatsAppNotifier>();

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

app.MapControllers();
app.MapHub<WhatsAppHub>("/hubs/whatsapp");

await app.UseArchonAccessSyncAsync();

app.Run();
