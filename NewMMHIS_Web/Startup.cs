using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewMMHIS_Web.Models;
using NewMMHIS_Web.Services;

namespace NewMMHIS_Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();

            // Short-lived contexts per operation; Blazor circuits must not share one DbContext across threads.
            services.AddDbContextFactory<mmhisContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("MMHISDatabase")));

            services.AddMemoryCache();
            services.Configure<ImageryOptions>(Configuration.GetSection("Imagery"));
            services.AddSingleton<RunCatalog>();
            services.AddSingleton<RunService>();
            services.AddSingleton<FrameImageService>();

            // Frame lists are large JSON arrays; images are already compressed and excluded by MIME type.
            services.AddResponseCompression(options => options.EnableForHttps = true);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseResponseCompression();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapImageryEndpoints();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
