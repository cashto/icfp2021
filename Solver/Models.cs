using IcfpUtils;
using System.Collections.Generic;
using System.Linq;

namespace Solver
{
    public class OptimizationBody
    {
        public List<int> selected { get; set; }
        public List<List<int>> solution { get; set; }
    }

    public class FigureBody
    {
        public List<List<int>> edges;
        public List<List<int>> vertices;
    }

    public class ProblemBody
    {
        public List<List<int>> hole;
        public int epsilon;
        public FigureBody figure;

        public List<Point2D> ProblemHole() => hole.Select(i => new Point2D(i[0], i[1])).ToList();
    }

    public class SolutionBody
    {
        public List<List<int>> vertices { get; set; }

        public List<Point2D> Vertices() => vertices.Select(i => new Point2D(i[0], i[1])).ToList();
    }

    public class ValidateResponseBody
    {
        public List<int> badBounds { get; set; }
        public List<int> badLengths { get; set; }
        public double dislikes { get; set; }
        public List<double> stretchFactors { get; set; }

        public bool IsWorseThan(ValidateResponseBody other)
        {
            if (other.IsValid && !this.IsValid)
            {
                return true;
            }

            return other.IsValid && this.IsValid && this.dislikes > other.dislikes;
        }

        public bool IsValid => !badBounds.Any() && !badLengths.Any();
    }

    public class SaveResponseBody
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int dislikes { get; set; }
    }
}
