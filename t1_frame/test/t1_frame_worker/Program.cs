
using Microsoft.Extensions.Configuration;
using t1_frame_worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
// Add Hangfire services.

var host = builder.Build();
host.Run();
