using AspNetCoreRateLimit;
using DBContexts;
using IService.BaseManage;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Service.BaseManage;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using WebApp.Filter;
using WebApp.Log;

namespace WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Log4net �ִ���
        /// </summary>
        public static ILoggerRepository repository { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            #region ���ݿ�����
            services.AddDbContext<SqlServerDBContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString("SqlServerConnection")));
            #endregion

            #region ����razor sdk������ʱ����
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            #endregion

            #region ֧�� Razor ҳ��
            services.AddRazorPages();
            #endregion

            services.AddControllers();

            #region Swagger����
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1.0.0",
                    Title = "WebAPI",
                    Description = "������Ӻ�ͬWebApi�ӿ�",
                    Contact = new OpenApiContact()
                    {
                        Name = "David Zhou",
                        Email = "1732182169@qq.com"
                    }
                });
            });
            #endregion

            #region IP��������

            // �洢IP�����������ù���
            services.AddMemoryCache();
            // ����
            services.Configure<IpRateLimitOptions>(options =>
            {
                //options.EnableEndpointRateLimiting = true; // true����˼��IP���ƻ�Ӧ���ڵ������õ�Endpoint��,false��ֻ���������нӿ�Endpointֻ��Ϊ*
                options.HttpStatusCode = 429; // ��������֮����ͻ��˷��ص�HTTP״̬��
                options.RealIpHeader = "X-Real-IP";
                options.ClientIdHeader = "X-ClientId";
                options.QuotaExceededResponse = new QuotaExceededResponse // ��������֮����ͻ��˷��ص�����
                {
                    Content = "{{\"code\":-1,\"msg\":\"���ʹ���Ƶ�������Ժ�����\"}}",
                    ContentType = "application/json",
                    StatusCode = 429
                };
                // ���ƹ���
                options.GeneralRules = new List<RateLimitRule>
                {
                    new RateLimitRule
                    {
                        //Endpoint = "*:/signtoken",// ����ƥ�����ֻ���ǩ��token�ӿ�
                        Endpoint = "*",
                        Period = "1s",// ���� s�� m���� hʱ d��
                        Limit = 10,// ����
                    }
                };
            });

            // ע��������͹���洢
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            // the clientId/clientIp resolvers use it.
            services.AddHttpContextAccessor();
            // ���ã���������Կ��������
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            #endregion

            #region ��������
            //ע�⣺�ŵ�services.AddMvc()֮ǰ
            services.AddCors(options => options.AddPolicy("AllowAll",
            builder =>
            {
                builder.SetIsOriginAllowed(_ => true)
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            }));

            #endregion

            #region ���MVC����ȡ��Ĭ���շ�����
            services.AddMvc()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });
            #endregion

            #region Api ���ݸ�ʽ����
            services.AddControllersWithViews().AddNewtonsoftJson(options =>
            {
                //����ѭ������
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                //ʹ���շ���ʽ
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                //����ʱ���ʽ
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            });
            #endregion

            #region �м��ע��

            #region ��������
            //��Ա��ͬ����
            services.AddTransient<IPersonService, PersonService>();
            //��Ա��������
            services.AddTransient<IECPersonFileInfoService, ECPersonFileInfoService>();

            //log������
            services.AddSingleton<ILoggerHelper, LoggerHelper>();

            //log4net
            repository = LogManager.CreateRepository("WebApp");//��Ŀ����
            XmlConfigurator.Configure(repository,new FileInfo("Log4net.config"));//ָ�������ļ�
            #endregion

            #endregion

            #region ������
            services.AddControllers(option => {
                //ȫ���쳣������
                option.Filters.Add(typeof(GlobalExceptionFilter));
            });
            #endregion
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //��������
            app.UseCors("AllowAll");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // ��������������app.UseRouting();֮ǰ
            app.UseIpRateLimiting();

            app.UseRouting();

            app.UseAuthorization();

            #region Swagger����
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiHelp V1");
                //������ø�Ŀ¼Ϊswagger,����ֵ�ÿ�
                options.RoutePrefix = string.Empty;
            });
            #endregion

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
