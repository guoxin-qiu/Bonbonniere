using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;
using WebAPIs.Filters.Swashbuckle;
using WebAPIs.Helpers;
using WebAPIs.Services;

namespace WebAPIs
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                if (appAssembly != null)
                {
                    builder.AddUserSecrets(appAssembly, optional: true);
                }
            }

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);
            var appSettings = appSettingsSection.Get<AppSettings>();

            services.AddDbContext<Models.DataContext>(opt => opt.UseInMemoryDatabase("TodoList"));

            services.AddMvc()
                .AddXmlSerializerFormatters()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            ConfigureCORS(services, appSettings);
            ConfigureAutoMapper(services);
            ConfigureAuthentication(services, appSettings);
            ConfigureSwagger(services);
            ConfigureApiVersioning(services);
            ConfigureDI(services);
        }

        private void ConfigureAutoMapper(IServiceCollection services)
        {
            services.AddAutoMapper();
        }

        private void ConfigureDI(IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
        }

        private void ConfigureApiVersioning(IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine
                (
                    new HeaderApiVersionReader("api-version"),
                    new QueryStringApiVersionReader("api-version")
                );
            });
        }

        private void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
           {
               options.SwaggerDoc("v1", new Info
               {
                   Version = "v1",
                   Title = "Bonbonniere Web API V1",
                   Description = "Bonbonniere Web API V1",
                   TermsOfService = "None",
                   Contact = new Contact
                   {
                       Name = "Denis",
                       Email = string.Empty,
                       Url = string.Empty
                   },
                   License = new License
                   {
                       Name = "Use under LICX",
                       Url = "https://example.com/license"
                   }
               });

               // Set the comments path for the Swagger JSON and UI.
               var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
               var xmlPath = Path.Combine(System.AppContext.BaseDirectory, xmlFile);
               options.IncludeXmlComments(xmlPath);

               //options.AddSecurityDefinition("oauth2", new ApiKeyScheme
               //{
               //    Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
               //    In = "header",
               //    Name = "Authorization",
               //    Type = "apiKey"
               //});
               //options.OperationFilter<SecurityRequirementsOperationFilter>();

               options.OperationFilter<AddAuthorizationHeaderParameterOperationFilter>();
           });
        }

        private void ConfigureAuthentication(IServiceCollection services, AppSettings appSettings)
        {
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                        var userId = int.Parse(context.Principal.Identity.Name);
                        var user = userService.GetById(userId);
                        if (user == null)
                        {
                            context.Fail("Unauthorized");
                        }

                        return Task.CompletedTask;
                    }
                };
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
        }

        private void ConfigureCORS(IServiceCollection services, AppSettings appSettings)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder => builder.WithOrigins(appSettings.Origins)
                    .AllowCredentials().AllowAnyMethod().AllowAnyHeader());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // Do not expose Swagger interface in production
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bonbonniere Web API V1");
                });
            }
            else
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseCors("AllowSpecificOrigin");
            app.UseAuthentication();

            app.UseMvc();
        }
    }
}
