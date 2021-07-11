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
            return Ok(Validate(problem, solution.vertices));
        }

        private ValidateResponseBody Validate(ProblemBody problem, List<List<int>> solutionVertices)
        {
            return new ValidateResponseBody()
            {
                badBounds = Enumerable.Range(0, problem.figure.edges.Count).Where(i => Program.IsBadBound(problem, solutionVertices, i)).ToList(),
                badLengths = Enumerable.Range(0, problem.figure.edges.Count).Where(i => Program.IsBadLength(problem, solutionVertices, i)).ToList(),
                dislikes = Program.Dislikes(problem.ProblemHole(), solutionVertices.Select(i => new Point2D(i[0], i[1])).ToList())
            };
        }

        [HttpPost]
        [Route("bruteforce/{id}")]
        public IActionResult BruteForce(int id)
        {
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(10));
            var result = Solver.BruteForce.Optimize(id, cancellationSource.Token);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("incrementalforce/{id}")]
        public IActionResult IncrementalForce(int id, [FromBody] OptimizationBody body)
        {
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(10));
            var problem = JsonConvert.DeserializeObject<ProblemBody>(System.IO.File.ReadAllText($"{Program.ProblemsRoot}\\problem{id}.json"));

            var initialValidation = Validate(problem, body.solution);
            var result = IncrementalBruteForce.Execute(problem, cancellationSource.Token, body);
            
            if (result == null || Validate(problem, result.vertices).IsWorseThan(initialValidation))
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("optimize/{id}")]
        public IActionResult Optimize(int id, [FromBody] OptimizationBody body)
        {
            var problem = JsonConvert.DeserializeObject<ProblemBody>(System.IO.File.ReadAllText($"{Program.ProblemsRoot}\\problem{id}.json"));
            var initialValidation = Validate(problem, body.solution);

            var currentSolution = body.solution.Select(i => new Point2D(i[0], i[1])).ToList();
            currentSolution = Optimizer.Optimize(problem, GetCancellationToken(TimeSpan.FromSeconds(5)), currentSolution, optimizeForDislikes: true);
            currentSolution = Optimizer.Optimize(problem, GetCancellationToken(TimeSpan.FromSeconds(5)), currentSolution, optimizeForDislikes: false);
            var newSolution = new SolutionBody() { vertices = currentSolution.Select(i => new List<int>() { (int)i.x, (int)i.y }).ToList() };

            if (currentSolution == null || Validate(problem, newSolution.vertices).IsWorseThan(initialValidation))
            {
                return NotFound();
            }

            return Ok(newSolution);
        }

        [HttpPost]
        [Route("refine/{id}")]
        public IActionResult Refine(int id, [FromBody] OptimizationBody body)
        {
            var problem = JsonConvert.DeserializeObject<ProblemBody>(System.IO.File.ReadAllText($"{Program.ProblemsRoot}\\problem{id}.json"));
            var initialValidation = Validate(problem, body.solution);

            var currentSolution = body.solution.Select(i => new Point2D(i[0], i[1])).ToList();
            currentSolution = Solver.Refine.Optimize(problem, TimeSpan.FromSeconds(10), currentSolution);
            var newSolution = new SolutionBody() { vertices = currentSolution.Select(i => new List<int>() { (int)i.x, (int)i.y }).ToList() };

            if (currentSolution == null || Validate(problem, newSolution.vertices).IsWorseThan(initialValidation))
            {
                return NotFound();
            }

            return Ok(newSolution);
        }

        [HttpPost]
        [Route("corner/{id}")]
        public IActionResult Corner(int id, [FromBody] OptimizationBody body)
        {
            var problem = JsonConvert.DeserializeObject<ProblemBody>(System.IO.File.ReadAllText($"{Program.ProblemsRoot}\\problem{id}.json"));
            var initialValidation = Validate(problem, body.solution);

            var currentSolution = body.solution.Select(i => new Point2D(i[0], i[1])).ToList();
            currentSolution = Solver.Corner.Optimize(problem, GetCancellationToken(TimeSpan.FromSeconds(10)), body);
            var newSolution = new SolutionBody() { vertices = currentSolution.Select(i => new List<int>() { (int)i.x, (int)i.y }).ToList() };

            if (currentSolution == null || Validate(problem, newSolution.vertices).IsWorseThan(initialValidation))
            {
                return NotFound();
            }

            return Ok(newSolution);
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
                var problemFilename = $"{Program.ProblemsRoot}\\problem{id}.json";
                var problem = JsonConvert.DeserializeObject<ProblemBody>(System.IO.File.ReadAllText(problemFilename));
                var validation = Validate(problem, solution.vertices);
                if (!validation.IsValid)
                {
                    throw new Exception("Cannot submit invalid solution!");
                }

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

        private static CancellationToken GetCancellationToken(TimeSpan timeout)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(timeout);
            return cancellationTokenSource.Token;
        }
    }
}
