using Microsoft.VisualStudio.TestTools.UnitTesting;
using IcfpUtils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class IcfpUtilsTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var x = Lisp.Parse("(deffn factorial (x) (if (= x 0) (1) (factorial (- x 1))))");
            
            foreach (var stack in x.Walk().Where(s => s.Last().IsLeaf))
            {
                foreach (var item in stack.Skip(1))
                {
                    var print = item.IsLeaf ? item.ToString() : "*";
                    Console.Write($"{print} ");
                }

                Console.WriteLine();
            }
        }

        [TestMethod]
        public void TestLargest()
        {
            var strings = new List<string>() { "hello", "this", "is", "a", "test", "of", "the", "maximum", "method" };
            var ans = strings.Largest((i) => i.Length);
            Assert.AreEqual("maximum", ans);
        }
    }
}
