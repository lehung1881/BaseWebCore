using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BASE.Service.Core.Web
{
    public class BaseStartupServices
    {
        public static void ConfigureServices(WebApplicationBuilder builder, IConfiguration configuration = null, bool isSetAuthorization = true)
        {
            //Khởi tạo cấu hình vào configuration
            InitConfigGlobal(builder);

            //Cho phép query với command là StoredProcedure
            AppContext.SetSwitch("Npgsql.EnableStoredProcedureCompatMode", true);

            //Thêm option này để bỏ validate model ở đầu controller
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            //Xử lý file upload lớn
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 60000000;
            });

            //Cấu hình để khi convert json không bị chuyển sang camelCase
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            builder.Services.AddCors();

            //Cấu hình JWT
            if (isSetAuthorization && configuration != null) 
            {
                builder.Services.AddJwtAuthentication(configuration);
            }

            //HttpContext
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            //Add core Services
            //builder.Services.UseCoreServices();

            //Add BL Services
            //builder.Services.UseCoreBLServices();

            //Add services
            builder.Services.UseWebCoreServices(configuration);
        }

        public static void ConfigureApp(WebApplication app)
        {
            app.UseHttpsRedirection();

            //Xử lý Authen và Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            //Tránh bị chặn lỗi blocked
            app.UseCors(opt => opt.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

            app.MapControllers();
        }

        /// <summary>
        /// Khởi tạo cấu hình
        /// </summary>
        /// <param name="builder"></param>
        public static void InitConfigGlobal(WebApplicationBuilder builder)
        {
            try
            {
                string pathConfig = GetPathConfigCommon(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "appsettings.json");
                builder.Configuration.AddJsonFile(pathConfig, optional: false, reloadOnChange: true);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"BaseStartupServices. InitConfigGlobal Exception: <{ex}>.");
            }
        }


        /// <summary>
        /// Lấy đường dẫn
        /// </summary>
        /// <param name="folderRoot"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string GetPathConfigCommon(string folderRoot, string fileName)
        {
            var configCommon = string.Empty;

            try
            {
                Console.WriteLine($"GetPathConfigCommon. folderRoot: <{folderRoot}>; fileName: <{fileName}>.");

                var countCheck = 0;
                var existFile = false;

                do
                {
                    configCommon = Path.Combine(folderRoot, "config", fileName);

                    Console.WriteLine($"GetPathConfigCommon. configCommon path: {configCommon}");

                    if (!File.Exists(configCommon))
                    {
                        countCheck++;
                        configCommon = string.Empty;
                        folderRoot = Directory.GetParent(folderRoot).FullName;
                    }
                    else
                    {
                        existFile = true;
                    }
                }
                while (!existFile && countCheck < 15);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetPathConfigCommon Exception: {ex.Message}");
            }

            return configCommon;
        }
    }
}
