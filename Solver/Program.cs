using Newtonsoft.Json;
using IcfpUtils;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Solver
{
    public static class Program
    {
        public static readonly string GitRoot = @"C:\Users\cashto\Documents\GitHub\icfp2021\";
        public static readonly string ProblemsRoot = GitRoot + "\\problems";
        public static readonly string WorkRoot = GitRoot + "\\work";

        class SearchState
        {
            public List<Point2D?> vertices;

            public SearchState(int nodeCount)
            {
                vertices = Enumerable.Range(0, nodeCount).Select(i => (Point2D?)null).ToList();
            }

            public SearchState(IncrementalForceBody body)
            {
                vertices = body.solution.Select(i => (Point2D?)new Point2D(i[0], i[1])).ToList();
                foreach (var i in body.selected)
                {
                    vertices[i] = null;
                }
            }
        }

        public static void Main(string[] args)
        {
            Server.Start(args);
        }

        // The first, last, and only algorithm you'll ever need.
        public static SolutionBody BruteForce(int problemId, CancellationToken cancel)
        { 
            var problem = JsonConvert.DeserializeObject<ProblemBody>(File.ReadAllText($"{ProblemsRoot}\\problem{problemId}.json"));
            var problemBounds = CalculateHoleBounds(problem);
            var problemHole = problem.hole.Select(i => new Point2D(i[0], i[1])).ToList();
            var interiorPoints = CalculateInteriorPoints(problemBounds, problemHole).ToList();

            var results = Algorithims.Search(
                new SearchState(problem.figure.vertices.Count),
                new DepthFirstSearch<SearchState, NoMove>(),
                cancel,
                (currentState) => NextState(currentState, problem, problemHole, interiorPoints));

            var scoredResults =
                from result in results
                where result.Depth == problem.figure.vertices.Count
                let solution = new SolutionBody()
                {
                    vertices = result.State.vertices.Select(i => new List<int>() { (int)i.Value.x, (int)i.Value.y }).ToList()
                }
                let dislikes = Dislikes(problem, solution)
                select new { Solution = solution, Dislikes = dislikes };

            var listedScoredResults = scoredResults.ToList();

            var sortedResults =
                from result in listedScoredResults
                orderby result.Dislikes
                select result.Solution;

            return sortedResults.FirstOrDefault();
        }

        public static SolutionBody IncrementalBruteForce(int problemId, CancellationToken cancel, IncrementalForceBody body)
        {
            var problem = JsonConvert.DeserializeObject<ProblemBody>(File.ReadAllText($"{ProblemsRoot}\\problem{problemId}.json"));
            var problemBounds = CalculateHoleBounds(problem);
            var problemHole = problem.hole.Select(i => new Point2D(i[0], i[1])).ToList();
            var interiorPoints = CalculateInteriorPoints(problemBounds, problemHole).ToList();

            var initialState = new SearchState(body);

            var results = Algorithims.Search(
                initialState,
                new DepthFirstSearch<SearchState, NoMove>(),
                cancel,
                (currentState) => NextIncrementalState(currentState, problem, problemHole, interiorPoints, body));

            var scoredResults =
                from result in results
                where result.Depth == body.selected.Count
                let solution = new SolutionBody()
                {
                    vertices = result.State.vertices.Select(i => new List<int>() { (int)i.Value.x, (int)i.Value.y }).ToList()
                }
                let dislikes = Dislikes(problem, solution)
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
            List<Point2D> interiorPoints)
        {
            if (currentState.Depth == problem.figure.vertices.Count)
            {
                yield break;
            }

            var mostConstainedVertex = CalculateMostConstrainedVertex(problem, currentState.State);
            
            foreach (var point in interiorPoints)
            {
                currentState.State.vertices[mostConstainedVertex] = point;
                if (IsValidSolutionSoFar(problem, problemHole, currentState.State))
                {
                    //foreach (var vertex in currentState.State.vertices)
                    //{
                    //    Console.Write(vertex.HasValue ? vertex.Value.ToString() : "null");
                    //    Console.Write(' ');
                    //}
                    //Console.WriteLine();

                    yield return currentState.Create(currentState.State, new NoMove());
                }
                
                currentState.State.vertices[mostConstainedVertex] = null;
            }

            yield break;
        }

        private static IEnumerable<SearchNode<SearchState, NoMove>> NextIncrementalState(
            SearchNode<SearchState, NoMove> currentState,
            ProblemBody problem,
            List<Point2D> problemHole,
            List<Point2D> interiorPoints,
            IncrementalForceBody body)
        {
            if (currentState.Depth == body.selected.Count)
            {
                yield break;
            }

            var mostConstainedVertex = CalculateMostConstrainedVertex(problem, currentState.State, body);

            foreach (var point in interiorPoints)
            {
                currentState.State.vertices[mostConstainedVertex] = point;
                if (IsValidSolutionSoFar(problem, problemHole, currentState.State, body))
                {
                    foreach (var vertex in currentState.State.vertices)
                    {
                        Console.Write(vertex.HasValue ? vertex.Value.ToString() : "null");
                        Console.Write(' ');
                    }
                    Console.WriteLine();

                    yield return currentState.Create(currentState.State, new NoMove());
                }

                currentState.State.vertices[mostConstainedVertex] = null;
            }

            yield break;
        }

        private static int CalculateMostConstrainedVertex(ProblemBody problem, SearchState searchState, IncrementalForceBody body = null)
        {
            var vertexSource = body == null ? Enumerable.Range(0, problem.figure.vertices.Count) : body.selected;
            
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

        private static bool IsValidSolutionSoFar(
            ProblemBody problem,
            List<Point2D> problemHole,
            SearchState searchState,
            IncrementalForceBody body = null)
        {
            foreach (var edge in problem.figure.edges)
            {
                var p1 = searchState.vertices[edge[0]];
                var p2 = searchState.vertices[edge[1]];

                var edgeIsRelevant =
                    body == null ? true :
                    body.selected.Contains(edge[0]) || body.selected.Contains(edge[1]);

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

        private static Point2D CalculateHoleBounds(ProblemBody problem)
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

        private static IEnumerable<Point2D> CalculateInteriorPoints(
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

        public static int Dislikes(ProblemBody problem, SolutionBody solution)
        {
            return (int)problem.hole
                .Select(hole => solution.vertices
                    .Select(vertex =>
                        new LineSegment2D(
                            new Point2D(hole[0], hole[1]),
                            new Point2D(vertex[0], vertex[1])).SquaredLength)
                    .Min())
                .Sum();
        }

        public static bool IsBadBound(ProblemBody problem, SolutionBody solution, int edgeIdx)
        {
            var edge = problem.figure.edges[edgeIdx];
            var p1 = solution.vertices[edge[0]];
            var p2 = solution.vertices[edge[1]];

            return IsBadBound(
                problem,
                problem.hole.Select(i => new Point2D(i[0], i[1])).ToList(),
                new Point2D(p1[0], p1[1]),
                new Point2D(p2[0], p2[1]),
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

        public static bool IsBadLength(ProblemBody problem, SolutionBody solution, int edgeIdx)
        {
            var edge = problem.figure.edges[edgeIdx];
            var p1 = solution.vertices[edge[0]];
            var p2 = solution.vertices[edge[1]];
            return IsBadLength(problem, new Point2D(p1[0], p1[1]), new Point2D(p2[0], p2[1]), edge);
        }

        public static bool IsBadLength(ProblemBody problem, Point2D begin, Point2D end, List<int> edge)
        {
            var solutionSegment = new IcfpUtils.LineSegment2D(begin, end);

            var p1 = problem.figure.vertices[edge[0]];
            var p2 = problem.figure.vertices[edge[1]];
            var problemSegment = new IcfpUtils.LineSegment2D(
                new Point2D(p1[0], p1[1]),
                new Point2D(p2[0], p2[1]));

            var stretchFactor = Math.Abs(solutionSegment.SquaredLength / problemSegment.SquaredLength - 1);

            return stretchFactor > problem.epsilon / 1000000.0;
        }
    }
}
