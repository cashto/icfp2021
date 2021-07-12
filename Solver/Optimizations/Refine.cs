using Newtonsoft.Json;
using IcfpUtils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Solver
{
    class RefineMove
    {
        public int FixedEdge { get; set; }
    }

    static class Refine
    {
        static Random random = new Random();

        public static List<Point2D> Optimize(
            ProblemBody problem,
            TimeSpan timeout,
            OptimizationBody optimizationBody)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(timeout);
            var cancellationToken = cancellationTokenSource.Token;

            // Select all vertices to be initially set
            optimizationBody.selected = new List<int>();

            var initialState = new SearchState(problem, optimizationBody, findYellowNodes: true);
            var results = Algorithims.Search(
                initialState,
                BestFirstSearch.Create<SearchState, RefineMove>((lhs, rhs) => lhs.State < rhs.State, 100000),
                cancellationToken,
                (searchNode) => RefineNextState(searchNode, problem));

            var result = results.Largest(result => -result.State.yellowEdges.Count);
            Console.WriteLine($"Refine: {initialState.yellowEdges.Count} -> {result.State.yellowEdges.Count}");

            return result.State.vertices.Cast<Point2D>().ToList();
        }

        private static List<Point2D> RefineSearchDeltas = CreateSearchDeltas();
        private static List<Point2D> CreateSearchDeltas()
        {
            var radius = 5;

            var searchDeltas =
                from x in Enumerable.Range(-radius, 2 * radius + 1)
                from y in Enumerable.Range(-radius, 2 * radius + 1)
                let point = new Point2D(x, y)
                let length = new LineSegment2D(Point2D.Zero, point).SquaredLength
                where length <= (radius + 0.5) * (radius + 0.5)
                where point != Point2D.Zero
                orderby length
                select point;

            return searchDeltas.ToList();
        }

        private static IEnumerable<SearchNode<SearchState, RefineMove>> RefineNextState(
            SearchNode<SearchState, RefineMove> searchNode,
            ProblemBody problem)
        {
            var currentState = searchNode.State;
            var vertexCount = problem.figure.vertices.Count;
            //Program.PrintCurrentState(searchNode);
            //Console.WriteLine("   " + string.Join(",", searchNode.Moves.Select(i => i.FixedEdge)));

            foreach (var yellowEdgeIdx in currentState.yellowEdges
                .Where(i => !searchNode.Moves.Any(j => i == j.FixedEdge))
                .Shuffle())
            {
                var yellowEdge = problem.figure.edges[yellowEdgeIdx];
                for (var ii = 0; ii < 2; ++ii)
                {
                    var yellowNodeIdx = yellowEdge[ii];
                    var yellowNode = currentState.vertices[yellowNodeIdx].Value;

                    var otherNodeIdx = yellowEdge[1 - ii];
                    var otherNode = currentState.vertices[otherNodeIdx].Value;

                    foreach (var delta in RefineSearchDeltas.Where(i =>
                        !Program.IsBadLength(problem, yellowNode + i, otherNode, yellowEdge)))
                    {
                        var newState = new SearchState(currentState, yellowNode + delta, yellowNodeIdx, problem);
                        Debug.Assert(!newState.yellowEdges.Contains(yellowEdgeIdx));
                        if (
                            newState.neighborNodes[yellowNodeIdx].All(i => !Program.IsBadBound(
                                problem,
                                newState.problemHole,
                                yellowNode,
                                newState.vertices[i].Value,
                                new List<int>() { yellowNodeIdx, i })))
                        {
                            //Console.WriteLine($"   Moving node {yellowNodeIdx} by {delta} to {yellowNode + delta}");
                            yield return searchNode.Create(newState, new RefineMove() { FixedEdge = yellowEdgeIdx });
                        }
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
    }
}
