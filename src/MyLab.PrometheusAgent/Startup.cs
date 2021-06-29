using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.PrometheusAgent.Services;
using MyLab.StatusProvider;
using MyLab.WebErrors;

namespace MyLab.PrometheusAgent
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(c => c.AddConsole());
            services.AddControllers(c => c.AddExceptionProcessing());
            services.AddAppStatusProviding(Configuration as IConfigurationRoot);
            services.AddSingleton<IScrapeConfigProvider, ScrapeConfigProvider>();
            services.AddSingleton<ITargetsMetricProvider, TargetsMetricProvider>();
            services.AddSingleton<IMetricReportBuilder, MetricReportBuilder>();
            services.AddSingleton<TargetsReportService>();
            services.Configure<PrometheusAgentOptions>(Configuration.GetSection("PROMETHEUS_AGENT"));

#if DEBUG
            services.Configure<ExceptionProcessingOptions>(o => o.HideError = false);
#endif
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
