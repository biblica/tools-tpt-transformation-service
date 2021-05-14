using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TptMain.Http;
using TptMain.InDesign;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.ParatextProjects;
using TptMain.Projects;
using TptMain.Toolbox;

namespace TptMain
{
    /// <summary>
    /// Class used to configure the ASP.NET request services pipeline.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public IConfiguration Configuration => _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add database service for tracking Preview Jobs
            services.AddDbContext<TptServiceContext>(
                options => options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection")),
               ServiceLifetime.Singleton);

            services.AddSingleton<IPreviewJobValidator, PreviewJobValidator>();
            services.AddSingleton<IJobManager, JobManager>();
            services.AddSingleton<IProjectManager, ProjectManager>();

            services.AddSingleton<InDesignScriptRunner>();
            services.AddSingleton<ParatextApi>();
            services.AddSingleton<ParatextProjectService>();
            services.AddSingleton<JobScheduler>();
            services.AddSingleton<TemplateManager>();
            services.AddSingleton<WebRequestFactory>();


            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}