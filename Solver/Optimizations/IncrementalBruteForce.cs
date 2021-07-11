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
            var interiorPoints = Program.CalculateInteriorPoints(problemBounds, problem.ProblemHole()).Shuffle();

            var initialState = new SearchState(problem, optimizationBody);

            var results = Algorithims.Search(
                initialState,
                new DepthFirstSearch<SearchState, NoMove>(),
                cancel,
                (currentState) => NextState(currentState, problem, interiorPoints, optimizationBody));

            var scoredResults =
                from result in results
                where result.Depth == optimizationBody.selected.Count
                let solution = new SolutionBody()
                {
                    vertices = result.State.vertices.Select(i => new List<int>() { (int)i.Value.x, (int)i.Value.y }).ToList()
                }
                let dislikes = Program.Dislikes(initialState.problemHole, result.State.vertices.Cast<Point2D>().ToList())
                select new { Solution = solution, Dislikes = dislikes };

            var listedScoredResults = scoredResults.ToList();

            var sortedResults =
                from result in listedScoredResults
                orderby result.Dislikes
                select result.Solution;

            return sortedResults.FirstOrDefault();
        }

        private static IEnumerable<SearchNode<SearchState, NoMove>> NextState(
            SearchNode<SearchState, NoMove> searchNode,
            ProblemBody problem,
            List<Point2D> interiorPoints,
            OptimizationBody optimizationBody)
        {
            if (searchNode.Depth == optimizationBody.selected.Count)
            {
                yield break;
            }

            var currentState = searchNode.State;
            var mostConstainedVertex = Program.CalculateMostConstrainedVertex(problem, currentState, optimizationBody);

            foreach (var point in interiorPoints)
            {
                currentState.vertices[mostConstainedVertex] = point;
                if (Program.IsValidSolutionSoFar(problem, currentState.problemHole, currentState, optimizationBody))
                {
                    Program.PrintCurrentState(currentState);
                    yield return searchNode.Create(currentState, new NoMove());
                }

                currentState.vertices[mostConstainedVertex] = null;
            }

            yield break;
        }
    }
}
