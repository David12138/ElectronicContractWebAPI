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
            #endregion

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
