using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAppMiddlewareChangingRequestBody
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

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebAppMiddlewareChangingRequestBody", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebAppMiddlewareChangingRequestBody v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.Use(async (context, next) => {
                try
                {
                    string userIdFromHeader = context.Request.Headers["userid"].ToString();
                    if (!string.IsNullOrEmpty(userIdFromHeader))
                    {
                        var stream = context.Request.Body;
                        var originalContent = await new StreamReader(stream).ReadToEndAsync();
                        dynamic objRequestBody = JsonConvert.DeserializeObject<dynamic>(originalContent);

                        if (!string.IsNullOrEmpty(userIdFromHeader) && objRequestBody != null)
                        {
                            if(userIdFromHeader == "0")
                            {
                                var errorBodyJson = JsonConvert.SerializeObject(new {  Error = 400, Message = "Ok" });
                                var erroBytes = Encoding.UTF8.GetBytes(errorBodyJson);
                                var errorBodyStream = new MemoryStream(erroBytes);
                                context.Response.StatusCode = 401; //UnAuthorized
                                await context.Response.Body.WriteAsync(erroBytes, 0, erroBytes.Length);
                                return;
                            }

                            objRequestBody.UserId = userIdFromHeader;
                            var newBodyJson = JsonConvert.SerializeObject(objRequestBody);
                            var requestInputInBytes = Encoding.UTF8.GetBytes(newBodyJson);
                            var newBodyStream = new MemoryStream(requestInputInBytes);
                            context.Request.Body = newBodyStream;
                        }
                        else
                        {
                            var newBodyJson = JsonConvert.SerializeObject(objRequestBody);
                            var requestInputInBytes = Encoding.UTF8.GetBytes(newBodyJson);
                            var newBodyStream = new MemoryStream(requestInputInBytes);
                            context.Request.Body = newBodyStream;
                        }
                    }

                    Console.WriteLine("Ooopa aqui o middleware shoow");
                    await next.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ooopa aqui o middleware shoow");
                    await next.Invoke();
                }


            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
