using System;

namespace IcfpUtils
{
    public struct Point2D
    {
        public double x { get; set; }
        public double y { get; set; }

        public Point2D(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }

        public static bool operator== (Point2D lhs, Point2D rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y;
        }

        public static bool operator !=(Point2D lhs, Point2D rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Point2D?;
            return other != null && this == other;
        }
    }

    public class LineSegment2D
    {
        public Point2D Begin { get; set; }
        public Point2D End { get; set; }

        public LineSegment2D(Point2D p1, Point2D p2)
        {
            Begin = p1;
            End = p2;
        }

        public Point2D? Intersection(LineSegment2D other)
        {
            var myLine = new Line2D(this);
            var otherLine = new Line2D(other);

            var intersection = myLine.Intersection(otherLine);
            if (intersection.HasValue &&
                IsBounded(intersection.Value) && 
                other.IsBounded(intersection.Value))
            {
                return intersection;
            }

            return null;
        }

        public bool Collinear(Point2D point)
        {
            return new Line2D(this).Collinear(point);
        }

        public bool ContainsPoint(Point2D point)
        {
            return IsBounded(point) && Collinear(point);
        }

        private bool IsBounded(Point2D point)
        {
            return
                point.x >= this.Left.x &&
                point.x <= this.Right.x &&
                point.y >= this.Top.y &&
                point.y <= this.Bottom.y;
        }

        public Point2D Midpoint => new Point2D((Begin.x + End.x) / 2, (Begin.y + End.y) / 2);

        public double SquaredLength => DeltaX * DeltaX + DeltaY * DeltaY;
        public double DeltaX => Begin.x - End.x;
        public double DeltaY => Begin.y - End.y;

        public Point2D Left => Begin.x.CompareTo(End.x) < 0 ? Begin : End;
        public Point2D Right => Begin.x.CompareTo(End.x) >= 0 ? Begin : End;
        public Point2D Top => Begin.y.CompareTo(End.y) < 0 ? Begin : End;
        public Point2D Bottom => Begin.y.CompareTo(End.y) >= 0 ? Begin : End;

        public override string ToString()
        {
            return $"{Begin} - {End}";
        }
    }

    public class Line2D
    {
        private double a;
        private double b;
        private double c;

        public Line2D(LineSegment2D segment)
        {
            a = segment.DeltaY;
            b = -segment.DeltaX;
            c = -(a * segment.Begin.x + b * segment.Begin.y);
        }

        public Point2D? Intersection(Line2D other)
        {
            var t = this.a * other.b - this.b * other.a;
            if (t == 0)
            {
                return null;
            }

            var ans = new Point2D(
                (this.b * other.c - other.b * this.c) / t,
                (other.a * this.c - this.a * other.c) / t);

            return ans;
        }

        public bool Collinear(Point2D point)
        {
            return Math.Abs(a * point.x + b * point.y + c) < 1e-9;
        }
    }
}