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
                //�]�m�s���ƾڭݮe�ʯŧO�M�����ǦC�Ƶ{�ǡA�H�K����x�@�~���ѧ��ꪺ���ĭt��
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                //set the recommended JSON options
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
                {
                    //�R�O��B�z�̤j�W��
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(3),
                    //�ư����ζW��
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(2),
                    //���C���߶��j
                    QueuePollInterval = TimeSpan.Zero,
                    //�ϥα��˪��j���ŧO
                    UseRecommendedIsolationLevel = true,
                    //�T�Υ�����
                    DisableGlobalLocks = true
                })
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.ServerCount) //�A�Ⱦ��ƶq
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.RecurringJobCount) //���ȼƶq
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.RetriesCount) //���զ���
            //.UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.EnqueuedCountOrNull)//���C�ƶq
            //.UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.FailedCountOrNull)//���Ѽƶq
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.EnqueuedAndQueueCount) //���C�ƶq
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.ScheduledCount) //�p�����ȼƶq
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.ProcessingCount) //���椤�����ȼƶq
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.SucceededCount) //���\�@�~�ƶq
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.FailedCount) //���Ѽƶq
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.DeletedCount) //�R���ƶq
            .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.AwaitingCount) //���ݥ��ȼƶq
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
            //Fire-and-forget tasks�S�ٮg�ᤣ�z����
            var jobId1 =  backgroundJobs.Enqueue(() => serive.Log($"Hangfire Start!"));
            //�������
            var jobId2 = backgroundJobs.Schedule(() => serive.Log($"Delayed!{DateTime.Now} | "), TimeSpan.FromMinutes(1));
            //Cron Expressions
            //https://docs.oracle.com/cd/E12058_01/doc/doc.1014/e12030/cron_expressions.htm
            //*/2 every 2 minutes
            //*���0
            //���O�{������_�⪺�C����� �ӬO�u��ɶ��������
            //�Ҧp�{���O10�I01������ �o��02���N�|�]�@���M��04,06�|����o��

            //25/3 * ? * *
            //��25����C�T�����]�@�� 25�A28�A31
            //https://www.freeformatter.com/cron-expression-generator-quartz.html#
            //6��5��4�I23��1��
            //1 23 4 5 JUN ?
            //�C�몺�g�� 4�I23��1��
            //1 23 4 ? * SUN
            recurringJobManager.AddOrUpdate("Some-GUID", () => serive.Log($"RecurringJob!�G{DateTime.Now}"), "1 23 4 ? * SUN");
        }
    }
}
