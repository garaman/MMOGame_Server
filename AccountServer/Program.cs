using AccountServer.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedDB;

namespace AccountServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.DictionaryKeyPolicy = null;
            });

            builder.Services.AddDbContext<AppDbContext>(options => 
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            builder.Services.AddDbContext<SharedDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("SharedConnection"));
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();                
            }
           
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            
            app.UseAuthorization();
            app.MapControllers();
            
            app.Run();
        }
    }
}
