using Newtonsoft.Json;
using IcfpUtils;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Solver
{
    static class IncrementalBruteForce
    {
        public static SolutionBody Execute(ProblemBody problem, CancellationToken cancel, OptimizationBody optimizationBody)
        {
            var problemBounds = Program.CalculateHoleBounds(problem);
            var problemHole = problem.ProblemHole();
            var interiorPoints = Program.CalculateInteriorPoints(problemBounds, problemHole).Shuffle();

            var initialState = new SearchState(optimizationBody);

            var results = Algorithims.Search(
                initialState,
                new DepthFirstSearch<SearchState, NoMove>(),
                cancel,
                (currentState) => NextState(currentState, problem, problemHole, interiorPoints, optimizationBody));

            var scoredResults =
                from result in results
                where result.Depth == optimizationBody.selected.Count
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
            List<Point2D> problemHole,
            List<Point2D> interiorPoints,
            OptimizationBody optimizationBody)
        {
            if (currentState.Depth == optimizationBody.selected.Count)
            {
                yield break;
            }

            var mostConstainedVertex = Program.CalculateMostConstrainedVertex(problem, currentState.State, optimizationBody);

            foreach (var point in interiorPoints)
            {
                currentState.State.vertices[mostConstainedVertex] = point;
                if (Program.IsValidSolutionSoFar(problem, problemHole, currentState.State, optimizationBody))
                {
                    Program.PrintCurrentState(currentState.State);
                    yield return currentState.Create(currentState.State, new NoMove());
                }

                currentState.State.vertices[mostConstainedVertex] = null;
            }

            yield break;
        }
    }
}
