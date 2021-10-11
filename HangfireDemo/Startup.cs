using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.SqlServer;

namespace HangfireDemo
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
            // Add Hangfire services.
            services.AddHangfire(configuration => configuration
                //設置新的數據兼容性級別和類型序列化程序，以便為後台作業提供更緊湊的有效負載
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                //set the recommended JSON options
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
                {
                    //命令批處理最大超時
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    //滑動隱形超時
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    //隊列輪詢間隔
                    QueuePollInterval = TimeSpan.Zero,
                    //使用推薦的隔離級別
                    UseRecommendedIsolationLevel = true,
                    //禁用全局鎖
                    DisableGlobalLocks = true
                })
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.ServerCount) //服務器數量
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.RecurringJobCount) //任務數量
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.RetriesCount) //重試次數
            //.UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.EnqueuedCountOrNull)//隊列數量
            //.UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.FailedCountOrNull)//失敗數量
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.EnqueuedAndQueueCount) //隊列數量
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.ScheduledCount) //計劃任務數量
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.ProcessingCount) //執行中的任務數量
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.SucceededCount) //成功作業數量
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.FailedCount) //失敗數量
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.DeletedCount) //刪除數量
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.AwaitingCount) //等待任務數量
                );
           
            services.AddHangfireServer();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "HangfireDemo", Version = "v1" });
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IBackgroundJobClient backgroundJobs,
            IRecurringJobManager recurringJobManager,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HangfireDemo v1"));
            }
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseHangfireDashboard();
            //Fire-and-forget tasks又稱射後不理任務
            var jobId1 =  backgroundJobs.Enqueue(() => Console.WriteLine($"Hangfire Start!：{DateTime.Now}"));
            //延遲執行
            var jobId2 = backgroundJobs.Schedule(() => Console.WriteLine($"Delayed!：{DateTime.Now}"), TimeSpan.FromMinutes(2));
            //Cron Expressions
            //https://docs.oracle.com/cd/E12058_01/doc/doc.1014/e12030/cron_expressions.htm
            //every 3 minutes
            //*/3 * * * *
            //
            recurringJobManager.AddOrUpdate(DateTime.Now.Ticks.ToString(), () => Console.WriteLine($"RecurringJob!：{DateTime.Now}"), "*/3 * * * *");
        }
    }
}
