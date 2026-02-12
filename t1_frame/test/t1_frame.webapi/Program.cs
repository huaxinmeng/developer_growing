using MassTransit;
using System.Reflection;
using t1_frame.webapi.Filters;

namespace t1_frame.webapi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers(options =>
            {
                //options.Filters.Add(typeof(SampleAsyncActionFilter));
                //options.Filters.Add(typeof(SampleActionFilter));
            });
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddScoped<LoggingResponseHeaderFilterService>();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.SetInMemorySagaRepositoryProvider();
                var entryAssembly = Assembly.GetEntryAssembly();
                x.AddConsumers(entryAssembly);
                x.AddSagaStateMachines(entryAssembly);
                x.AddSagas(entryAssembly);
                x.AddActivities(entryAssembly);

                //x.UsingInMemory((conntext, cfg) =>
                //{
                //    cfg.ConfigureEndpoints(conntext);
                //});

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host("192.168.1.214", "/", h => {
                        h.Username("nick");
                        h.Password("123");
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}