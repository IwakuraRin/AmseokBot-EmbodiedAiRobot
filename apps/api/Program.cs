using NasForWindows.Api.Features.System.GetStatus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWindowsService(options => options.ServiceName = "NasForWindows API");
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapSystemStatus();
app.Run();

public partial class Program;
