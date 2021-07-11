using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using IcfpUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using Solver;

namespace UnitTests
{
    [TestClass]
    public class SolverTests
    {
        [TestMethod]
        public void Foo()
        {
            var id = 3;
            var edge = 4;

            var problemFilename = $"{Program.ProblemsRoot}\\problem{id}.json";
            var problem = JsonConvert.DeserializeObject<ProblemBody>(System.IO.File.ReadAllText(problemFilename));

            var solutionFilename = $"{Program.WorkRoot}\\solution{id}.json";
            var solution = JsonConvert.DeserializeObject<SolutionBody>(System.IO.File.ReadAllText(solutionFilename));

            Assert.IsFalse(Program.IsBadBound(problem, solution.vertices, edge));
        }

        [TestMethod]
        public void Bar()
        {
            var id = 14;

            var problemFilename = $"{Program.ProblemsRoot}\\problem{id}.json";
            var problem = JsonConvert.DeserializeObject<ProblemBody>(System.IO.File.ReadAllText(problemFilename));

            var solutionFilename = $"{Program.WorkRoot}\\solution{id}.json";
            var solution = JsonConvert.DeserializeObject<SolutionBody>(System.IO.File.ReadAllText(solutionFilename));

            var badEdges = Enumerable.Range(0, problem.figure.edges.Count).Where(i => Program.IsBadBound(problem, solution.vertices, i)).ToList();

            Assert.AreEqual(0, badEdges.Count);
        }

        [TestMethod]
        public void Problem14BottomPoint()
        {
            var id = 14;

            var problemFilename = $"{Program.ProblemsRoot}\\problem{id}.json";
            var problem = JsonConvert.DeserializeObject<ProblemBody>(System.IO.File.ReadAllText(problemFilename));

            var problemHole = problem.ProblemHole();
            Assert.IsTrue(Program.IsInside(problemHole, new Point2D(10, 10)));
        }

        private ProblemBody GetProblem(int id)
        {
            var problemFilename = $"{Program.ProblemsRoot}\\problem{id}.json";
            return JsonConvert.DeserializeObject<ProblemBody>(System.IO.File.ReadAllText(problemFilename));
        }

        private SolutionBody GetSolution(int id)
        {
            var problemFilename = $"{Program.WorkRoot}\\solution{id}.json";
            return JsonConvert.DeserializeObject<SolutionBody>(System.IO.File.ReadAllText(problemFilename));
        }
    }
}
