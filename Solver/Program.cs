using Newtonsoft.Json;
using IcfpUtils;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Solver
{
    public class SearchState
    {
        public List<Point2D?> vertices;
        public int vertexCount;
        public int edgeCount;

        public List<List<int>> neighborNodes;
        public List<Point2D> problemHole;

        public SearchState(
            SearchState previousSearchState,
            Point2D newVertex,
            int newVertexIdx,
            ProblemBody problem)
        {
            vertices = previousSearchState.vertices.ToList();
            vertices[newVertexIdx] = newVertex;
            vertexCount = previousSearchState.vertexCount + 1;
            edgeCount = problem.figure.edges.Count(edge => vertices[edge[0]].HasValue && vertices[edge[1]].HasValue);

            neighborNodes = previousSearchState.neighborNodes;
            problemHole = previousSearchState.problemHole;
        }

        public SearchState(int nodeCount, List<Point2D> problemHole, List<List<int>> neighborNodes = null)
        {
            this.problemHole = problemHole;
            this.neighborNodes = neighborNodes;
            vertices = Enumerable.Range(0, nodeCount).Select(i => (Point2D?)null).ToList();
        }

        public SearchState(OptimizationBody optimizationBody)
        {
            vertices = optimizationBody.solution.Select(i => (Point2D?)new Point2D(i[0], i[1])).ToList();
            foreach (var i in optimizationBody.selected)
            {
                vertices[i] = null;
            }
        }

        public static bool operator <(SearchState lhs, SearchState rhs)
        {
            if (lhs.vertexCount == rhs.vertexCount)
            {
                return lhs.edgeCount < rhs.edgeCount;
            }

            return lhs.vertexCount < rhs.vertexCount;
        }

        public static bool operator >(SearchState lhs, SearchState rhs)
        {
            return rhs < lhs;
        }

        public override string ToString()
        {
            return $"SearchNode:({vertexCount}, {edgeCount})";
        }
    }

    public static class Program
    {
        public static readonly string GitRoot = @"C:\Users\cashto\Documents\GitHub\icfp2021\";
        public static readonly string ProblemsRoot = GitRoot + "\\problems";
        public static readonly string WorkRoot = GitRoot + "\\work";

        public static void Main(string[] args)
        {
            Server.Start(args);
        }

        public static void PrintCurrentState(SearchState state)
        {
            //foreach (var vertex in state.vertices)
            //{
            //    Console.Write(vertex.HasValue ? vertex.Value.ToString() : "null");
            //    Console.Write(' ');
            //}
            //Console.WriteLine();
        }

        public static int CalculateMostConstrainedVertex(ProblemBody problem, SearchState searchState, OptimizationBody optimizationBody = null)
        {
            var vertexSource = optimizationBody == null ? Enumerable.Range(0, problem.figure.vertices.Count) : optimizationBody.selected;

            var fixedVertexIndexes = searchState.vertices.Where(i => i != null);

            var longestEdgesWithOneFixedVertex =
                from edge in problem.figure.edges
                where searchState.vertices[edge[0]].HasValue ^ searchState.vertices[edge[1]].HasValue
                where vertexSource.Contains(searchState.vertices[edge[0]].HasValue ? edge[1] : edge[0])
                let edgeLength = ComputeEdgeLength(problem, edge)
                orderby edgeLength descending
                select edge;

            if (!longestEdgesWithOneFixedVertex.Any()) 
            {
                return problem.figure.edges.Largest(edge => ComputeEdgeLength(problem, edge))[0];
            }

            var longestEdge = longestEdgesWithOneFixedVertex.First();

            return searchState.vertices[longestEdge[0]].HasValue ? longestEdge[1] : longestEdge[0];
        }

        private static double ComputeEdgeLength(ProblemBody problem, List<int> edge)
        {
            var p1 = problem.figure.vertices[edge[0]];
            var p2 = problem.figure.vertices[edge[1]];
            return new LineSegment2D(new Point2D(p1[0], p1[1]), new Point2D(p2[0], p2[1])).SquaredLength;
        }

        private static int CalculateMostConstrainedVertex2(ProblemBody problem, SearchState searchState, OptimizationBody optimizationBody = null)
        {
            var vertexSource = optimizationBody == null ? Enumerable.Range(0, problem.figure.vertices.Count) : optimizationBody.selected;

            var vertexIndexes =
                from vertexIndex in vertexSource
                where !searchState.vertices[vertexIndex].HasValue
                let constraintsInSet = problem.figure.edges.Count(edge =>
                    (edge[0] == vertexIndex || edge[1] == vertexIndex) &&
                    (searchState.vertices[edge[0]].HasValue || searchState.vertices[edge[1]].HasValue))
                let constraints = problem.figure.edges.Count(edge =>
                    (edge[0] == vertexIndex || edge[1] == vertexIndex))
                orderby constraintsInSet descending, constraints descending
                select vertexIndex;

            return vertexIndexes.First();
        }

        public static bool IsValidSolutionSoFar(
            ProblemBody problem,
            List<Point2D> problemHole,
            SearchState searchState,
            OptimizationBody optimizationBody = null)
        {
            foreach (var edge in problem.figure.edges)
            {
                var p1 = searchState.vertices[edge[0]];
                var p2 = searchState.vertices[edge[1]];

                var edgeIsRelevant =
                    optimizationBody == null ? true :
                    optimizationBody.selected.Contains(edge[0]) || optimizationBody.selected.Contains(edge[1]);

                if (p1.HasValue && p2.HasValue && edgeIsRelevant)
                {
                    if (IsBadLength(problem, p1.Value, p2.Value, edge) ||
                        IsBadBound(problem, problemHole, new LineSegment2D(p1.Value, p2.Value), edge))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static Point2D CalculateHoleBounds(ProblemBody problem)
        {
            var max_x = 0;
            var max_y = 0;

            foreach (var vertex in problem.hole)
            {
                var x = vertex[0];
                var y = vertex[1];

                if (x > max_x) { max_x = x; }
                if (y > max_y) { max_y = y; }
            }

            return new Point2D(max_x, max_y);
        }

        public static IEnumerable<Point2D> CalculateInteriorPoints(
            Point2D problemBounds,
            List<Point2D> problemHole)
        {
            foreach (var y in Enumerable.Range(0, (int)problemBounds.y + 1))
            {
                foreach (var x in Enumerable.Range(0, (int)problemBounds.x + 1))
                {
                    var point = new Point2D(x, y);
                    if (IsInside(problemHole, point))
                    {
                        yield return point;
                    }
                }
            }
        }

        public static bool IsInside(List<Point2D> hole, Point2D point)
        {
            var segments = 0;
            var testSegment = new LineSegment2D(point, new Point2D(100000, point.y));

            foreach (var holeIdx in Enumerable.Range(0, hole.Count))
            {
                var segment = new LineSegment2D(hole[holeIdx], hole[(holeIdx + 1) % hole.Count]);
                if (segment.ContainsPoint(point))
                {
                    return true;
                }

                var intersection = segment.Intersection(testSegment);
                if (intersection.HasValue)
                {
                    if (intersection.Value != segment.Begin && intersection.Value != segment.End ||
                        intersection.Value == segment.Begin && point.y < segment.End.y ||
                        intersection.Value == segment.End && point.y < segment.Begin.y)
                    {
                        ++segments;
                    }
                }
            }

            return (segments % 2) == 1;
        }

        public static int Dislikes(List<Point2D> problemHole, List<Point2D> solution)
        {
            return (int)
                problemHole
                .Select(hole => solution.Select(vertex => new LineSegment2D(hole, vertex).SquaredLength).Min())
                .Sum();
        }

        public static bool IsBadBound(ProblemBody problem, List<List<int>> solutionVertices, int edgeIdx)
        {
            var edge = problem.figure.edges[edgeIdx];
            var p1 = solutionVertices[edge[0]];
            var p2 = solutionVertices[edge[1]];

            return IsBadBound(
                problem,
                problem.ProblemHole(),
                new Point2D(p1[0], p1[1]),
                new Point2D(p2[0], p2[1]),
                edge);
        }

        public static bool IsBadBound(ProblemBody problem, List<Point2D> problemHole, List<Point2D> solution, int edgeIdx)
        {
            var edge = problem.figure.edges[edgeIdx];

            return IsBadBound(
                problem,
                problemHole,
                solution[edge[0]],
                solution[edge[1]],
                edge);
        }

        public static bool IsBadBound(
            ProblemBody problem,
            List<Point2D> problemHole,
            LineSegment2D solutionSegment,
            List<int> edge)
        {
            if (!IsInside(problemHole, solutionSegment.Midpoint))
            {
                return true;
            }

            foreach (var i in Enumerable.Range(0, problem.hole.Count))
            {
                var p1 = problem.hole[i];
                var p2 = problem.hole[(i + 1) % problem.hole.Count];
                var holeSegment = new IcfpUtils.LineSegment2D(
                    new Point2D(p1[0], p1[1]),
                    new Point2D(p2[0], p2[1]));
                var intersection = solutionSegment.Intersection(holeSegment);

                if (intersection.HasValue &&
                    !holeSegment.ContainsPoint(solutionSegment.Begin) &&
                    !holeSegment.ContainsPoint(solutionSegment.End) &&
                    !solutionSegment.ContainsPoint(holeSegment.Begin) &&
                    !solutionSegment.ContainsPoint(holeSegment.End))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsBadBound(
            ProblemBody problem,
            List<Point2D> problemHole,
            Point2D begin,
            Point2D end,
            List<int> edge)
        {
            var solutionSegment = new LineSegment2D(begin, end);

            if (!IsInside(problemHole, solutionSegment.Begin))
            {
                return true;
            }

            if (!IsInside(problemHole, solutionSegment.End))
            {
                return true;
            }

            return IsBadBound(problem, problemHole, solutionSegment, edge);
        }

        public static bool IsBadLength(ProblemBody problem, List<List<int>> solutionVertices, int edgeIdx)
        {
            var edge = problem.figure.edges[edgeIdx];
            var p1 = solutionVertices[edge[0]];
            var p2 = solutionVertices[edge[1]];
            return IsBadLength(problem, new Point2D(p1[0], p1[1]), new Point2D(p2[0], p2[1]), edge);
        }

        public static bool IsBadLength(ProblemBody problem, Point2D begin, Point2D end, List<int> edge)
        {
            return StretchFactor(problem, begin, end, edge) > problem.epsilon;
        }

        // Can be directly compared to epsilon (ie, already includes scale of x1,000,000)
        public static double StretchFactor(ProblemBody problem, Point2D begin, Point2D end, List<int> edge)
        {
            var solutionSegment = new IcfpUtils.LineSegment2D(begin, end);

            var p1 = problem.figure.vertices[edge[0]];
            var p2 = problem.figure.vertices[edge[1]];
            var problemSegment = new IcfpUtils.LineSegment2D(
                new Point2D(p1[0], p1[1]),
                new Point2D(p2[0], p2[1]));

            return Math.Abs(solutionSegment.SquaredLength / problemSegment.SquaredLength - 1) * 1000000.0;
        }

        public static List<Point2D> RubberBand(
            ProblemBody problem,
            List<Point2D> solution,
            List<int> fixedVertices,
            int iterations,
            bool doBoundsCheck)
        {
            var currentSolution = solution.ToArray();

            foreach (var iter in Enumerable.Range(0, iterations))
            {
                var new_solution = currentSolution.ToArray();
                var k = 0.01;

                foreach (var edge in problem.figure.edges)
                {
                    var p1 = problem.figure.vertices[edge[0]];
                    var p2 = problem.figure.vertices[edge[1]];
                    var naturalLength = new Point2D(p1[0], p1[1]).Distance(new Point2D(p2[0], p2[1]));

                    var s1 = currentSolution[edge[0]];
                    var s2 = currentSolution[edge[1]];
                    var currentLength = s1.Distance(s2);

                    var ratio = currentLength / naturalLength - 1;

                    var dx = k * ratio * (s1.x - s2.x) / currentLength;
                    var dy = k * ratio * (s1.y - s2.y) / currentLength;

                    if (!fixedVertices.Contains(edge[0]))
                    {
                        new_solution[edge[0]].x -= dx;
                        new_solution[edge[0]].y -= dy;
                    }

                    if (!fixedVertices.Contains(edge[1]))
                    {
                        new_solution[edge[1]].x += dx;
                        new_solution[edge[1]].y += dy;
                    }
                }

                currentSolution = new_solution;
            }

            return currentSolution
                .Select(i => new Point2D(Math.Floor(i.x + 0.5), Math.Floor(i.y + 0.5)))
                .ToList();
        }
    }
}
