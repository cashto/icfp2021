using Newtonsoft.Json;
using IcfpUtils;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Solver
{
    public static class Optimizer
    {
        public static List<Point2D> Optimize(
            ProblemBody problem,
            CancellationToken cancellationToken,
            List<Point2D> currentSolution,
            bool optimizeForDislikes = false)
        {
            // Future: assert is valid solution: optimizationBody.solution

            var problemHole = problem.ProblemHole();

            while (!cancellationToken.IsCancellationRequested)
            {
                var newSolution = currentSolution.ToList();
                foreach (var vertexIdx in Enumerable.Range(0, newSolution.Count).Shuffle())
                {
                    var vertex = newSolution[vertexIdx];

                    var relevantEdgeIndexes = (
                        from index in Enumerable.Range(0, problem.figure.edges.Count)
                        let edge = problem.figure.edges[index]
                        where edge[0] == vertexIdx || edge[1] == vertexIdx
                        select index)
                        .ToList();

                    var bestMetric = ComputeDeltaMetric(problem, problemHole, newSolution, vertex, relevantEdgeIndexes, optimizeForDislikes);
                    var oldMetric = bestMetric;
                    var bestVertex = vertex;

                    foreach (var delta_y in Enumerable.Range(-3, 7))
                    {
                        foreach (var delta_x in Enumerable.Range(-3, 7))
                        {
                            var newVertex = new Point2D(vertex.x + delta_x, vertex.y + delta_y);
                            newSolution[vertexIdx] = newVertex;

                            var newMetric = ComputeDeltaMetric(problem, problemHole, newSolution, newVertex, relevantEdgeIndexes, optimizeForDislikes);
                            if (newMetric < bestMetric)
                            {
                                bestVertex = newVertex;
                                bestMetric = newMetric;
                            }
                        }
                    }

                    // Important!  Once we are done playing, reset the vertex
                    newSolution[vertexIdx] = vertex;

                    if (vertex != bestVertex)
                    {
                        Console.WriteLine($"Moving {vertex} to {bestVertex}. Before: {oldMetric}.  Now: {bestMetric}");
                        newSolution[vertexIdx] = bestVertex;
                    }
                }

                currentSolution = newSolution;
            }

            return currentSolution;
        }

        private static Metric ComputeDeltaMetric(
            ProblemBody problem,
            List<Point2D> problemHole,
            List<Point2D> solution,
            Point2D vertex,
            List<int> relevantEdgeIndexes,
            bool optimizeForDislikes)
        {
            if (relevantEdgeIndexes.Any(edgeIdx => Program.IsBadBound(problem, problemHole, solution, edgeIdx)))
            {
                return new Metric(double.PositiveInfinity, 0, 0);
            }

            var stretchFactorMetric = 0.0;
            var epsilon = problem.epsilon;
            foreach (var edgeIdx in relevantEdgeIndexes)
            {
                var edge = problem.figure.edges[edgeIdx];

                var stretchFactor = Program.StretchFactor(problem, solution[edge[0]], solution[edge[1]], edge);
                if (stretchFactor > epsilon)
                {
                    stretchFactorMetric += stretchFactor - epsilon;
                }
            }

            var ans = new Metric();
            ans.Metric1 = stretchFactorMetric;
            ans.Metric2 = Program.Dislikes(problemHole, solution);
            ans.Metric3 = problemHole.Min(hole => new LineSegment2D(vertex, hole).SquaredLength);

            return ans;
        }
    }
}
