using Newtonsoft.Json;
using IcfpUtils;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Solver
{
    static class Corner
    {
        public static List<Point2D> Optimize(ProblemBody problem, CancellationToken cancellationToken, OptimizationBody optimizationBody)
        {
            var solution = optimizationBody.solution.Select(i => new Point2D(i[0], i[1])).ToList();
            
            // Select all vertices to be initially set to null
            optimizationBody.selected = Enumerable.Range(0, problem.figure.vertices.Count).ToList();

            var results = Algorithims.Search(
                new SearchState(problem, optimizationBody),
                new DepthFirstSearch<SearchState, NoMove>(),
                cancellationToken,
                (currentState) => NextState(currentState, problem));

            var result = results
                .Where(i => i.Depth == problem.hole.Count)
                .FirstOrDefault();

            if (result != null)
            {
                foreach (var vertexIdx in Enumerable.Range(0, result.State.vertices.Count))
                {
                    solution[vertexIdx] = result.State.vertices[vertexIdx] ?? solution[vertexIdx];
                }
            }

            return solution;
        }

        private static IEnumerable<SearchNode<SearchState, NoMove>> NextState(
            SearchNode<SearchState, NoMove> searchNode,
            ProblemBody problem)
        {
            foreach (var vertexIdx in Enumerable.Range(0, problem.figure.vertices.Count)
                .Where(i => !searchNode.State.vertices[i].HasValue))
            {
                var point = problem.hole[searchNode.Depth];
                searchNode.State.vertices[vertexIdx] = new Point2D(point[0], point[1]);
                if (Program.IsValidSolutionSoFar(problem, searchNode.State.problemHole, searchNode.State))
                {
                    Program.PrintCurrentState(searchNode.State);
                    yield return searchNode.Create(searchNode.State, new NoMove());
                }

                searchNode.State.vertices[vertexIdx] = null;
            }
        }
    }
}
