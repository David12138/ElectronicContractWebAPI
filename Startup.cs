using DBContexts;
using IService.BaseManage;
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
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace WebApp
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
            #endregion

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
