using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;
using t1_frame.application.abp;
using t1_frame.entityframeworkcore.abp;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace t1_frame.webapi.abp
{
    [DependsOn(
    typeof(AbpAutofacModule),
    // typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAspNetCoreMvcModule),
        typeof(HostApplicationModule),
        typeof(HostEntityFrameworkModule))]
    public class HostWebHostModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // context.Services.AddControllers();
            ConfigureSwagger(context.Services);
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            var env = context.GetEnvironment();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllers();
            //});

            app.UseConfiguredEndpoints();

            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger(c => { c.RouteTemplate = "swagger/{documentName}/swagger.json"; });

            // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)
            app.UseSwaggerUI(options =>
            {
                // specifying the Swagger JSON endpoint.
                options.SwaggerEndpoint($"/swagger/{"v1"}/swagger.json", $"selfProject API {"v1"}");
                //options.IndexStream = () => Assembly.GetExecutingAssembly()
                //    .GetManifestResourceStream("selfProject.Web.Host.wwwroot.swagger.ui.index.html");
                options.DisplayRequestDuration(); // Controls the display of the request duration (in milliseconds) for "Try it out" requests.
            }); // URL: /swagger
        }

        private void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "selfProject API",
                    Description = "selfProject",
                    // uncomment if needed TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "selfProject",
                        Email = string.Empty,
                        Url = new Uri("https://twitter.com/aspboilerplate"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://github.com/aspnetboilerplate/aspnetboilerplate/blob/dev/LICENSE"),
                    }
                });
                options.DocInclusionPredicate((docName, description) => true);

                // Define the BearerAuth scheme that's in use
                options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme()
                {
                    Description =
                        "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                //add summaries to swagger
                //bool canShowSummaries = _appConfiguration.GetValue<bool>("Swagger:ShowSummaries");
                //if (canShowSummaries)
                //{
                //    var hostXmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                //    var hostXmlPath = Path.Combine(AppContext.BaseDirectory, hostXmlFile);
                //    options.IncludeXmlComments(hostXmlPath);

                //    //var applicationXml = $"selfProject.Application.xml";
                //    //var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXml);
                //    //options.IncludeXmlComments(applicationXmlPath);

                //    //var webCoreXmlFile = $"selfProject.Web.Core.xml";
                //    //var webCoreXmlPath = Path.Combine(AppContext.BaseDirectory, webCoreXmlFile);
                //    //options.IncludeXmlComments(webCoreXmlPath);
                //}
            });
        }
    }
}
