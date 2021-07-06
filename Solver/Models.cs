using System.Collections.Generic;

namespace Solver
{
    public class IncrementalForceBody
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
    }

    public class SolutionBody
    {
        public List<List<int>> vertices { get; set; }
    }

    public class ValidateResponseBody
    {
        public List<int> badBounds { get; set; }
        public List<int> badLengths { get; set; }
        public double dislikes { get; set; }
    }

    public class SaveResponseBody
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int dislikes { get; set; }
    }
}
