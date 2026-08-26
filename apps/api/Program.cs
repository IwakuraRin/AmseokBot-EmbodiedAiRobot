using NasForWindows.Api.Features.Audit;
using NasForWindows.Api.Features.System.GetStatus;
using NasForWindows.Api.Features.WebAccess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWindowsService(options => options.ServiceName = "NasForWindows API");
builder.Services.AddOpenApi();
builder.Services.AddWebAccessSecurity(builder.Environment, builder.Configuration);

var app = builder.Build();

await app.Services.InitializeWebAccessSecurityAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseMiddleware<AuthorizationAuditMiddleware>();
app.UseAuthorization();

app.MapWebAccessSecurityEndpoints();
app.MapAuditEndpoints();
app.MapSystemStatus();
app.Run();

public partial class Program;
