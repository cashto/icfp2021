using System;
using System.Collections.Generic;
using System.Text;

namespace Solver
{
    public struct Metric : IComparable<Metric>
    {
        // Sorry for the terrible names.  The metrics are arbitrary, in case they change later.  The are in order of decreasing priority.
        //
        // The intent is:
        //
        //     Metric1: sum(stretchFactor - epsilon) when stretchFactor > epsilon -- ie, how badly we're violating distance constraints
        //     Metric2: dislikes -- ie, how well we score
        //     Metric3: sum(min(distance(closest hole, vertex))) -- in case of Metric2 ties, how close are vertexes to the corners on average?
        //
        // Goal is to minimize this metric.
        //
        public double Metric1 { get; set; }
        public double Metric2 { get; set; }
        public double Metric3 { get; set; }

        public Metric(double metric1, double metric2, double metric3)
        {
            Metric1 = metric1;
            Metric2 = metric2;
            Metric3 = metric3;
        }

        public int CompareTo(Metric other)
        {
            if (Metric1 == other.Metric1)
            {
                if (Metric2 == other.Metric2)
                {
                    return Compare(Metric3, other.Metric3);
                }

                return Compare(Metric2, other.Metric2);
            }

            return Compare(Metric1, other.Metric1);
        }

        private int Compare(double lhs, double rhs)
        {
            return lhs == rhs ? 0 :
                lhs < rhs ? -1 : 
                1;
        }

        public static bool operator< (Metric lhs, Metric rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator> (Metric lhs, Metric rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        public override string ToString()
        {
            return $"({Metric1}, {Metric2}, {Metric3})";
        }
    }
}
