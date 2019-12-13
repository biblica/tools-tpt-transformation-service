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
using TptMain.Projects;
using TptMain.Toolbox;

namespace TptMain
{
    /// <summary>
    /// Class used to configure the ASP.NET request services pipeline.
    /// </summary>
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
            // In-memory database for tracking typesetting preview jobs
            services.AddDbContext<PreviewContext>(
                options => options.UseInMemoryDatabase("PreviewJobList"),
               ServiceLifetime.Singleton);

            services.AddSingleton<ScriptRunner>();
            services.AddSingleton<JobManager>();
            services.AddSingleton<JobScheduler>();
            services.AddSingleton<ProjectManager>();
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