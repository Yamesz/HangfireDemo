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
            services.AddSingleton<IService, Service>();

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
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(3),
                    //滑動隱形超時
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(2),
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
            //.UseNLogLogProvider()
                );
           
            services.AddHangfireServer(x=>new BackgroundJobServerOptions()
            {
                ServerTimeout = TimeSpan.FromMinutes(1)
            });

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
            IService serive,

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
            var jobId1 =  backgroundJobs.Enqueue(() => serive.Log($"Hangfire Start!"));
            //延遲執行
            var jobId2 = backgroundJobs.Schedule(() => serive.Log($"Delayed!{DateTime.Now} | "), TimeSpan.FromMinutes(1));
            //Cron Expressions
            //https://docs.oracle.com/cd/E12058_01/doc/doc.1014/e12030/cron_expressions.htm
            //*/2 every 2 minutes
            //*表示0
            //不是程式執行起算的每兩分鐘 而是真實時間的兩分鐘
            //例如程式是10點01分執行 這樣02分就會跑一次然後04,06會執行這樣

            //25/3 * ? * *
            //到25分後每三分鐘跑一次 25，28，31
            //https://www.freeformatter.com/cron-expression-generator-quartz.html#
            //6月5號4點23分1秒
            //1 23 4 5 JUN ?
            //每月的週日 4點23分1秒
            //1 23 4 ? * SUN
            recurringJobManager.AddOrUpdate("Some-GUID", () => serive.Log($"RecurringJob!：{DateTime.Now}"), "1 23 4 ? * SUN");
        }
    }
}
