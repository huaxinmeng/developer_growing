using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;
using System.Reflection.Emit;
using t1_frame.condition;
using t1_frame.core;
using t1_frame.core.TaskScheduler;
using t1_frame.entityframeworkcore;
using t1_frame.webapi.Controllers;

namespace t1_frame.webapi
{
    public class Program1
    {
        public static void Main1(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //builder.Host.ConfigureWebHostDefaults(webBuilder =>
            //{
            //    webBuilder.UseStartup<Startup>();
            //});
            //builder.WebHost.UseStartup<Startup>();

            // Add services to the container. 

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            Initialize();

            // Replace with your connection string.
            //var connectionString = "server=localhost;port=3306;user id=root;password=123456;database=t1_frame;Charset=utf8mb4;Allow User Variables=True;";

            // Replace with your server version and type.
            // Use 'MariaDbServerVersion' for MariaDB.
            // Alternatively, use 'ServerVersion.AutoDetect(connectionString)'.
            // For common usages, see pull request #1233.
            //var serverVersion = new MySqlServerVersion(new Version(8, 0, 31));

            // Replace 'YourDbContext' with the name of your own DbContext derived class.
            //builder.Services.AddDbContext<T1FrameDbContext>(
            //    dbContextOptions => dbContextOptions
            //        .UseMySql(connectionString, serverVersion)
            //        // The following three options help with debugging, but should
            //        // be changed or removed for production.
            //        .LogTo(Console.WriteLine, LogLevel.Information)
            //        .EnableSensitiveDataLogging()
            //        .EnableDetailedErrors()
            //);
            //builder.Services.AddSingleton<RabbitMQService>();
            //builder.Services.AddTransient<TestConsumer>();
            //builder.Services.AddSingleton<ScheduledTaskFactory>();
            //builder.Services.AddHostedService<TimeService>();

            builder.Services.AddDbContext<T1FrameDbContext>(opt => opt.UseInMemoryDatabase("t1_frame"));

            var app = builder.Build();

            //app.UseStartup<Startup>();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.MapGet("/", () => "Hello World!");

            //RouteGroupBuilder todoItems = app.MapGroup("/todoitems");
            //todoItems.MapPost("/", GetAllTodos);
            //var obj =  new Home();
            //var methodInfo = typeof(Home)
            //        .GetMethod("GetStr", BindingFlags.Public | BindingFlags.Instance);
            //.MakeGenericMethod(typeof(Home));
            //.Invoke(obj, new object[] {  });
            //var actionType = typeof(Func<>).MakeGenericType(typeof(string));
            //var bjk = (Func<string, string>)ddlt.CreateDelegate(typeof(Func<string, string>));
            //Delegate op = bjk;
            //bjk("12");
            //actionType = obj.GetStr;
            //var del = (Func<Home, string, string>)Delegate.CreateDelegate(typeof(Func<Home, string, string>), methodInfo);
            //var val = del(null, "");
            //var methodInfo = typeof(Home)
            //.GetMethod("GetStr", BindingFlags.Public | BindingFlags.Instance);
            //var del = (Func<Home, string, string>)Delegate.CreateDelegate(typeof(Func<Home, string, string>), methodInfo);
            //todoItems.MapGet("/Home", context => Task.FromResult(del(null, "yourStringArgument")));

            //var dynamicMethod = new DynamicMethod("GetStr", typeof(string), new[] {typeof(Home), typeof(string) }, typeof(Home).Module);
            //dynamicMethod.DefineParameter(1, ParameterAttributes.None, "source");
            //dynamicMethod.DefineParameter(2, ParameterAttributes.In, "name");
            //var ilGenerator = dynamicMethod.GetILGenerator();

            //ilGenerator.Emit(OpCodes.Ldarg_0);
            //ilGenerator.Emit(OpCodes.Ldarg_1);
            //ilGenerator.Emit(OpCodes.Callvirt, methodInfo);
            //ilGenerator.Emit(OpCodes.Ret);

            //var getStrDelegate = (Func<Home,string, string>)dynamicMethod.CreateDelegate(typeof(Func<Home,string, string>));
            //var homeInstance = new Home();
            //var result = getStrDelegate(homeInstance, "World");
            //Delegate op = bjk;
            //var st = bjk("12");

            //todoItems.MapGet("/Home", getStrDelegate);
            app.Run();


            static async Task<IResult> GetAllTodos(TodoDb db)
            {
                await Task.CompletedTask;
                //return TypedResults.Ok(await db.Todos.ToArrayAsync());
                return Results.Ok(db.Todos);
            }
        }

        private static void Initialize()
        {
            var manager = new NfpModuleManager();
            manager.Initialize(typeof(HostBootstrapperModule));
            manager.StartModules();
        }
    }
}