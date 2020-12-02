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
        /// Log4net 仓储库
        /// </summary>
        public static ILoggerRepository repository { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            #region 数据库连接
            services.AddDbContext<SqlServerDBContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString("SqlServerConnection")));
            #endregion

            #region 开启razor sdk的运行时编译
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            #endregion

            #region 支持 Razor 页面
            services.AddRazorPages();
            #endregion

            services.AddControllers();

            #region Swagger配置
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1.0.0",
                    Title = "WebAPI",
                    Description = "外包电子合同WebApi接口",
                    Contact = new OpenApiContact()
                    {
                        Name = "David Zhou",
                        Email = "1732182169@qq.com"
                    }
                });
            });
            #endregion

            #region IP限流配置

            // 存储IP计数器及配置规则
            services.AddMemoryCache();
            // 配置
            services.Configure<IpRateLimitOptions>(options =>
            {
                //options.EnableEndpointRateLimiting = true; // true，意思是IP限制会应用于单个配置的Endpoint上,false，只能限制所有接口Endpoint只能为*
                options.HttpStatusCode = 429; // 触发限制之后给客户端返回的HTTP状态码
                options.RealIpHeader = "X-Real-IP";
                options.ClientIdHeader = "X-ClientId";
                options.QuotaExceededResponse = new QuotaExceededResponse // 触发限制之后给客户端返回的数据
                {
                    Content = "{{\"code\":-1,\"msg\":\"访问过于频繁，请稍后重试\"}}",
                    ContentType = "application/json",
                    StatusCode = 429
                };
                // 限制规则
                options.GeneralRules = new List<RateLimitRule>
                {
                    new RateLimitRule
                    {
                        //Endpoint = "*:/signtoken",// 正则匹配规则，只针对签发token接口
                        Endpoint = "*",
                        Period = "1s",// 周期 s秒 m分钟 h时 d天
                        Limit = 10,// 次数
                    }
                };
            });

            // 注入计数器和规则存储
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            // the clientId/clientIp resolvers use it.
            services.AddHttpContextAccessor();
            // 配置（计数器密钥生成器）
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            #endregion

            #region 跨域设置
            //注意：放到services.AddMvc()之前
            services.AddCors(options => options.AddPolicy("AllowAll",
            builder =>
            {
                builder.SetIsOriginAllowed(_ => true)
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            }));

            #endregion

            #region 添加MVC服务，取消默认驼峰命名
            services.AddMvc()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });
            #endregion

            #region Api 数据格式配置
            services.AddControllersWithViews().AddNewtonsoftJson(options =>
            {
                //忽略循环引用
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                //使用驼峰样式
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                //设置时间格式
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            });
            #endregion

            #region 中间件注册

            #region 基础管理
            //人员合同服务
            services.AddTransient<IPersonService, PersonService>();
            //人员附件服务
            services.AddTransient<IECPersonFileInfoService, ECPersonFileInfoService>();

            //log帮助类
            services.AddSingleton<ILoggerHelper, LoggerHelper>();

            //log4net
            repository = LogManager.CreateRepository("WebApp");//项目名称
            XmlConfigurator.Configure(repository,new FileInfo("Log4net.config"));//指定配置文件
            #endregion

            #endregion

            #region 过滤器
            services.AddControllers(option => {
                //全局异常过滤器
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

            //跨域设置
            app.UseCors("AllowAll");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // 启用限流，放在app.UseRouting();之前
            app.UseIpRateLimiting();

            app.UseRouting();

            app.UseAuthorization();

            #region Swagger配置
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiHelp V1");
                //如果设置根目录为swagger,将此值置空
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
