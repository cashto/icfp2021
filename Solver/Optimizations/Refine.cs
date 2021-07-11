using Newtonsoft.Json;
using IcfpUtils;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Solver
{
    static class Refine
    {
        public static List<Point2D> Optimize(
            ProblemBody problem,
            TimeSpan timeout,
            List<Point2D> currentSolution)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(timeout);
            var cancellationToken = cancellationTokenSource.Token;
            var vertexCount = problem.figure.vertices.Count;

            var initialState = new SearchState(vertexCount, problem.ProblemHole(), GetNeighborNodes(problem));
            var results = Algorithims.Search(
                initialState,
                BestFirstSearch.Create<SearchState, NoMove>((lhs, rhs) => lhs.State < rhs.State, 100000),
                cancellationToken,
                (searchNode) => RefineNextState(searchNode, problem, currentSolution));

            var bestState = initialState;
            foreach (var result in results)
            {
                if (bestState < result.State)
                {
                    bestState = result.State;
                }
            }

            foreach (var vertexIdx in Enumerable.Range(0, vertexCount))
            {
                currentSolution[vertexIdx] = bestState.vertices[vertexIdx] ?? currentSolution[vertexIdx];
            }

            return currentSolution;
        }

        private static List<Point2D> RefineSearchDeltas = CreateSearchDeltas();
        private static List<Point2D> CreateSearchDeltas()
        {
            var radius = 2;

            var searchDeltas =
                from x in Enumerable.Range(-radius, 2 * radius + 1)
                from y in Enumerable.Range(-radius, 2 * radius + 1)
                let point = new Point2D(x, y)
                let length = new LineSegment2D(Point2D.Zero, point).SquaredLength
                where length <= (radius + 0.5) * (radius + 0.5)
                orderby length
                select point;

            return searchDeltas.ToList();
        }

        private static IEnumerable<SearchNode<SearchState, NoMove>> RefineNextState(
            SearchNode<SearchState, NoMove> searchNode,
            ProblemBody problem,
            List<Point2D> currentSolution)
        {
            var currentState = searchNode.State;
            var vertexCount = problem.figure.vertices.Count;
            //Console.WriteLine(currentState);

            var undeterminedVertexIndexes =
                from i in Enumerable.Range(0, vertexCount)
                where !searchNode.State.vertices[i].HasValue
                where IsAdjacentToCurrentSet(searchNode, i)
                orderby currentState.neighborNodes[i].Count
                select i;

            foreach (var newVertexIdx in undeterminedVertexIndexes.ToList())
            {
                foreach (var delta in RefineSearchDeltas)
                {
                    var newVertex = currentSolution[newVertexIdx] + delta;
                    var newState = new SearchState(currentState, newVertex, newVertexIdx, problem);
                    if (Program.IsValidSolutionSoFar(problem, currentState.problemHole, newState))
                    {
                        yield return searchNode.Create(newState, new NoMove());
                    }
                }
            }

            yield break;
        }

        private static bool IsAdjacentToCurrentSet(SearchNode<SearchState, NoMove> searchNode, int vertexIndex)
        {
            return searchNode.Depth == 0 ? true :
                searchNode.State.neighborNodes[vertexIndex].Any(i => searchNode.State.vertices[i].HasValue);
        }

        private static List<List<int>> GetNeighborNodes(ProblemBody problem)
        {
            var ans = Enumerable.Range(0, problem.figure.vertices.Count)
                .Select(i => new List<int>())
                .ToList();

            foreach (var edge in problem.figure.edges)
            {
                ans[edge[0]].Add(edge[1]);
                ans[edge[1]].Add(edge[0]);
            }

            return ans;
        }
    }
}
