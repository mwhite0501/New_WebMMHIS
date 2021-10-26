using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NewMMHIS_Web.Data;
using NewMMHIS_Web.Models;
using NewMMHIS_Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Radzen;

namespace NewMMHIS_Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<mmhisContext>();
            services.AddSingleton<PageModel>();
            services.AddScoped<DialogService>(); //Radzen stuff
            services.AddScoped<NotificationService>(); //Radzen stuff
            services.AddScoped<TooltipService>(); //Radzen stuff
            services.AddScoped<ContextMenuService>(); //Radzen stuff
            services.AddHttpClient("mmhisweb", client => { 
                client.BaseAddress = new Uri("https://localhost:44310/");
            });

            services.AddSingleton<MmhisController>();
            services.AddMvc(setupAction: options => options.EnableEndpointRouting = false)
                .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseMvcWithDefaultRoute();

            //app.UseFileServer(new FileServerOptions
            //{

            //    FileProvider = new PhysicalFileProvider(@"C:\MMHISPaths"),
            //    RequestPath = new PathString("/MyPath"),
            //    EnableDirectoryBrowsing = true
            //});
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Data1"),
                RequestPath = new PathString("/MyPath1"),
                EnableDirectoryBrowsing = true
            });

            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Data2"),
                RequestPath = new PathString("/MyPath2"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Data3"),
                RequestPath = new PathString("/MyPath3"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Data4"),
                RequestPath = new PathString("/MyPath4"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Data5"),
                RequestPath = new PathString("/MyPath5"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Data6"),
                RequestPath = new PathString("/MyPath6"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Data7"),
                RequestPath = new PathString("/MyPath7"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Data8"),
                RequestPath = new PathString("/MyPath8"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Data9"),
                RequestPath = new PathString("/MyPath9"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Dat10"),
                RequestPath = new PathString("/MyPath10"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Dat11"),
                RequestPath = new PathString("/MyPath11"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Dat12"),
                RequestPath = new PathString("/MyPath12"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Dat13"),
                RequestPath = new PathString("/MyPath13"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Dat14"),
                RequestPath = new PathString("/MyPath14"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Dat15"),
                RequestPath = new PathString("/MyPath15"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Dat16"),
                RequestPath = new PathString("/MyPath16"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Dat17"),
                RequestPath = new PathString("/MyPath17"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS Dat18"),
                RequestPath = new PathString("/MyPath18"),
                EnableDirectoryBrowsing = true
            });

            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE1"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE2"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE3"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE4"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE5"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE6"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE7"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE7"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE8"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE9"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE10"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE11"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE12"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE13"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE14"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE15"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE16"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE17"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE18"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE19"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE20"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(@"\\san2\MMHIS PAVE21"),
                RequestPath = new PathString("/MyPath"),
                EnableDirectoryBrowsing = true
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
