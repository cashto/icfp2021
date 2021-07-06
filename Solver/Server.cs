using IcfpUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using System;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Solver
{
    class Server
    {
        public static Dictionary<string, LispNode> Symbols = new Dictionary<string, LispNode>();
        
        public static void Start(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:8080/");
                });
    }


    // https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-5.0
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }

        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            const string rootFolder = @"C:\Users\cashto\Documents\GitHub\icfp2021\webroot";

            app.UseDefaultFiles();

            app.UseStaticFiles(
                new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(rootFolder)
                });
        }
    }

    [ApiController]
    [Route("api/brainwall")]
    public class BrainWallController : ControllerBase
    {
        public static HttpClient HttpClient = new HttpClient();
        public static string ApiKey = "e04a8265-9246-44cb-87db-89153b647e5c";

        [HttpPost]
        [Route("fetch/{id}")]
        public IActionResult Fetch(int id)
        {
            return PhysicalFile(
                $"{Program.ProblemsRoot}\\problem{id}.json",
                "application/json");
        }

        [HttpPost]
        [Route("load/{id}")]
        public IActionResult Load(int id)
        {
            var filename = $"{Program.WorkRoot}\\solution{id}.json";
            if (System.IO.File.Exists(filename))
            {
                return PhysicalFile(
                    filename,
                    "application/json");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("save/{id}")]
        public IActionResult Save(int id, [FromBody] SolutionBody solution)
        {
            var filename = $"{Program.WorkRoot}\\solution{id}.json";
            var result = new SaveResponseBody() { success = false };
            try
            {
                System.IO.File.WriteAllText(filename, JsonConvert.SerializeObject(solution));
                result.success = true;
            }
            catch
            {
                // this_is_fine_dog.jpg
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("validate/{id}")]
        public IActionResult Validate(int id, [FromBody] SolutionBody solution)
        {
            var problemFilename = $"{Program.ProblemsRoot}\\problem{id}.json";
            var problem = JsonConvert.DeserializeObject<ProblemBody>(System.IO.File.ReadAllText(problemFilename));
            var result = new ValidateResponseBody()
            {
                badBounds = Enumerable.Range(0, problem.figure.edges.Count).Where(i => Program.IsBadBound(problem, solution, i)).ToList(),
                badLengths = Enumerable.Range(0, problem.figure.edges.Count).Where(i => Program.IsBadLength(problem, solution, i)).ToList(),
                dislikes = Program.Dislikes(problem, solution)
            };

            return Ok(result);
        }

        [HttpPost]
        [Route("bruteforce/{id}")]
        public IActionResult BruteForce(int id)
        {
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(10));
            var result = Program.BruteForce(id, cancellationSource.Token);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("incrementalforce/{id}")]
        public IActionResult IncrementalForce(int id, [FromBody] IncrementalForceBody body)
        {
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(10));
            var result = Program.IncrementalBruteForce(id, cancellationSource.Token, body);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("submit/{id}")]
        public async Task<IActionResult> Submit(int id, [FromBody] SolutionBody solution)
        {
            var result = new SaveResponseBody()
            {
                success = false
            };

            try
            {
                var submissionDir = $"{Program.WorkRoot}\\submissions\\{id}";
                Directory.CreateDirectory(submissionDir);
                var submissionId = await SubmitToServer(id, solution);
                System.IO.File.WriteAllText($"{submissionDir}\\{submissionId}", JsonConvert.SerializeObject(solution));
                result.success = true;
            }
            catch (Exception e)
            {
                result.message = e.ToString();
            }

            return Ok(result);
        }

        private static async Task<string> SubmitToServer(
            int problemId,
            SolutionBody solution)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.Headers.Add("Authorization", "Bearer " + ApiKey);
            request.RequestUri = new Uri($"https://poses.live/api/problems/{problemId}/solutions");
            request.Content = new StringContent(JsonConvert.SerializeObject(solution), Encoding.UTF8, "application/json");

            var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return (string)(JObject.Parse(await response.Content.ReadAsStringAsync())["id"]);
        }
    }
}
