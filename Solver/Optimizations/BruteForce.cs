using Newtonsoft.Json;
using IcfpUtils;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Solver
{
    public static class BruteForce
    {
        // The first, last, and only algorithm you'll ever need.
        public static SolutionBody Optimize(int problemId, CancellationToken cancel)
        {
            var problem = JsonConvert.DeserializeObject<ProblemBody>(File.ReadAllText($"{Program.ProblemsRoot}\\problem{problemId}.json"));
            var problemBounds = problem.HoleBounds();
            var problemHole = problem.ProblemHole();
            var interiorPoints = Program.CalculateInteriorPoints(problemBounds, problemHole).Shuffle();

            var results = Algorithims.Search(
                new SearchState(problem.figure.vertices.Count, problemHole),
                new DepthFirstSearch<SearchState, NoMove>(),
                cancel,
                (currentState) => NextState(currentState, problem, interiorPoints));

            var scoredResults =
                from result in results
                where result.Depth == problem.figure.vertices.Count
                let solution = new SolutionBody()
                {
                    vertices = result.State.vertices.Select(i => new List<int>() { (int)i.Value.x, (int)i.Value.y }).ToList()
                }
                let dislikes = Program.Dislikes(problemHole, result.State.vertices.Cast<Point2D>().ToList())
                select new { Solution = solution, Dislikes = dislikes };

            var listedScoredResults = scoredResults.ToList();

            var sortedResults =
                from result in listedScoredResults
                orderby result.Dislikes
                select result.Solution;

            return sortedResults.FirstOrDefault();
        }

        private static IEnumerable<SearchNode<SearchState, NoMove>> NextState(
            SearchNode<SearchState, NoMove> currentState,
            ProblemBody problem,
            List<Point2D> interiorPoints)
        {
            if (currentState.Depth == problem.figure.vertices.Count)
            {
                yield break;
            }

            var mostConstainedVertex = Program.CalculateMostConstrainedVertex(problem, currentState.State);

            foreach (var point in interiorPoints)
            {
                currentState.State.vertices[mostConstainedVertex] = point;
                if (Program.IsValidSolutionSoFar(problem, currentState.State.problemHole, currentState.State))
                {
                    Program.PrintCurrentState(currentState.State);
                    yield return currentState.Create(currentState.State, new NoMove());
                }

                currentState.State.vertices[mostConstainedVertex] = null;
            }
        }
    }
}
