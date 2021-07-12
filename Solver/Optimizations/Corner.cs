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
            var initialState = new SearchState(problem, optimizationBody);
            var nodeDistances = Enumerable.Range(0, problem.figure.vertices.Count)
                .Select(i => Program.GetNodeDistances(problem, i, initialState.neighborNodes))
                .ToList();

            var holeOrder = Enumerable.Range(0, problem.hole.Count).Shuffle();

            var results = Algorithims.Search(
                initialState,
                new DepthFirstSearch<SearchState, NoMove>(),
                cancellationToken,
                (currentState) => NextState(currentState, problem, nodeDistances, holeOrder));

            var result = results
                .Where(i => i.Depth == problem.hole.Count)
                .FirstOrDefault();

            if (result == null)
            {
                return null;
            }

            var holeBounds = problem.HoleBounds();
            var random = new Random();
            foreach (var vertexIdx in Enumerable.Range(0, result.State.vertices.Count))
            {
                solution[vertexIdx] = result.State.vertices[vertexIdx] ??
                    new Point2D(random.Next((int)holeBounds.x), random.Next((int)holeBounds.y));
            }

            return solution;
        }

        private static IEnumerable<SearchNode<SearchState, NoMove>> NextState(
            SearchNode<SearchState, NoMove> searchNode,
            ProblemBody problem,
            List<List<double>> nodeDistances,
            List<int> holeOrder)
        {
            foreach (var vertexIdx in Enumerable.Range(0, problem.figure.vertices.Count)
                .Where(i => !searchNode.State.vertices[i].HasValue)
                .Shuffle())
            {
                var holeIdx = holeOrder[searchNode.Depth];
                var hole = problem.hole[holeIdx];
                var holePoint = new Point2D(hole[0], hole[1]);

                var otherVertexIndexes = Enumerable.Range(0, problem.figure.vertices.Count)
                    .Where(i => searchNode.State.vertices[i].HasValue);

                var isOk = true;
                foreach (var otherVertexIdx in otherVertexIndexes)
                {
                    var otherPoint = searchNode.State.vertices[otherVertexIdx].Value;
                    var distance = holePoint.Distance(otherPoint);
                    if (distance > nodeDistances[vertexIdx][otherVertexIdx])
                    {
                        isOk = false;
                        break;
                    }
                }

                //if (!isOk)
                //{
                //    continue;
                //}

                searchNode.State.vertices[vertexIdx] = holePoint;
                
                if (Program.IsValidSolutionSoFar(problem, searchNode.State.problemHole, searchNode.State))
                {
                    Program.PrintCurrentState(searchNode);
                    yield return searchNode.Create(searchNode.State, new NoMove());
                }

                searchNode.State.vertices[vertexIdx] = null;
            }
        }
    }
}
