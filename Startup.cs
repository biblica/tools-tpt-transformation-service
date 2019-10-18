using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using tools_tpt_transformation_service.Models;
using tools_tpt_transformation_service.InDesign;
using tools_tpt_transformation_service.Jobs;

namespace tools_tpt_transformation_service
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public IConfiguration Configuration { get => _configuration; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<PreviewContext>(
                options => options.UseInMemoryDatabase("PreviewJobList"),
               ServiceLifetime.Singleton);

            services.AddSingleton<ScriptRunner>();
            services.AddSingleton<JobManager>();
            services.AddSingleton<JobScheduler>();

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
