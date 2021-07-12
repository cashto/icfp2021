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
        public List<int> yellowNodes;
        public double yellowEdgeLengths;
        public List<int> yellowEdges;
        public bool findYellowNodes;

        // Along for the ride
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
            this.findYellowNodes = previousSearchState.findYellowNodes;
            FindYellowNodes(problem, vertices);
            neighborNodes = previousSearchState.neighborNodes;
            problemHole = previousSearchState.problemHole;
        }

        // Bruteforce ctor
        public SearchState(int nodeCount, List<Point2D> problemHole)
        {
            this.problemHole = problemHole;
            vertices = Enumerable.Range(0, nodeCount).Select(i => (Point2D?)null).ToList();
        }

        // Refine, corner, incremental brute force ctor
        public SearchState(ProblemBody problem, OptimizationBody optimizationBody, bool findYellowNodes = false)
        {
            this.findYellowNodes = findYellowNodes;
            this.problemHole = problem.ProblemHole();
            this.neighborNodes = Program.GetNeighborNodes(problem);
            vertices = optimizationBody.solution.Select(i => (Point2D?)new Point2D(i[0], i[1])).ToList();
            if (optimizationBody != null)
            foreach (var i in optimizationBody.selected)
            {
                vertices[i] = null;
            }

            FindYellowNodes(problem, vertices);
        }

        public static bool operator <(SearchState lhs, SearchState rhs)
        {
            if (lhs.yellowEdges.Count == rhs.yellowEdges.Count)
            {
                return lhs.yellowEdgeLengths < rhs.yellowEdgeLengths;
            }

            return lhs.yellowEdges.Count < rhs.yellowEdges.Count;
        }

        public static bool operator >(SearchState lhs, SearchState rhs)
        {
            return rhs < lhs;
        }

        public override string ToString()
        {
            return $"(YellowEdges={yellowEdges?.Count}, YellowNodes={yellowNodes?.Count}, YellowEdgeLengths={yellowEdgeLengths})";
        }

        private void FindYellowNodes(ProblemBody problem, List<Point2D?> solution)
        {
            if (!findYellowNodes)
            {
                return;
            }

            yellowNodes = new List<int>();
            yellowEdgeLengths = 0;
            yellowEdges = new List<int>();

            foreach (var edgeIdx in Enumerable.Range(0, problem.figure.edges.Count))
            {
                var edge = problem.figure.edges[edgeIdx];
                var begin = solution[edge[0]].Value;
                var end = solution[edge[1]].Value;

                if (Program.IsBadLength(problem, begin, end, edge))
                {
                    yellowEdges.Add(edgeIdx);
                    var edgeLength = Program.StretchFactor(problem, begin, end, edge);
                    if (edgeLength > yellowEdgeLengths)
                    {
                        yellowEdgeLengths = edgeLength;
                    }

                    if (!yellowNodes.Contains(edge[0]))
                    {
                        yellowNodes.Add(edge[0]);
                    }

                    if (!yellowNodes.Contains(edge[1]))
                    {
                        yellowNodes.Add(edge[1]);
                    }

                }
            }
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

        public static void PrintCurrentState<S, M>(SearchNode<S, M> searchNode)
        {
            //Console.WriteLine($"Depth:{searchNode.Depth}, CurrentState:{searchNode.State}");
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

        public static Point2D HoleBounds(this ProblemBody problem)
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

        public static double StretchFactor(ProblemBody problem, List<List<int>> solutionVertices, int edgeIdx)
        {
            var edge = problem.figure.edges[edgeIdx];
            var begin = new Point2D(solutionVertices[edge[0]][0], solutionVertices[edge[0]][1]);
            var end = new Point2D(solutionVertices[edge[1]][0], solutionVertices[edge[1]][1]);
            return StretchFactor(problem, begin, end, edge);
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

        public static List<List<int>> GetNeighborNodes(ProblemBody problem)
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

        public static List<double> GetNodeDistances(ProblemBody problem, int startNode, List<List<int>> neighborNodes)
        {
            var ans = Enumerable.Range(0, problem.figure.vertices.Count).Select(i => double.PositiveInfinity).ToList();
            var visitedNodes = Enumerable.Range(0, problem.figure.vertices.Count).Select(i => false).ToList();
            var nodes = new Queue<int>();
            
            nodes.Enqueue(startNode);
            ans[startNode] = 0;

            while (nodes.Any())
            {
                var node = nodes.Dequeue();
                visitedNodes[node] = true;

                var p1 = new Point2D(
                    problem.figure.vertices[node][0],
                    problem.figure.vertices[node][1]);

                foreach (var neighborNode in neighborNodes[node])
                {
                    if (!visitedNodes[neighborNode])
                    {
                        nodes.Enqueue(neighborNode);
                    }

                    var p2 = new Point2D(
                        problem.figure.vertices[neighborNode][0],
                        problem.figure.vertices[neighborNode][1]);

                    var distance = p1.Distance(p2);

                    var newDistance = ans[node] + distance;
                    if (newDistance < ans[neighborNode])
                    {
                        ans[neighborNode] = newDistance;
                    }
                }
            }

            return ans;
        }
    }
}
