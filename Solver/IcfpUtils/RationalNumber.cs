using System;
using System.Numerics;

namespace IcfpContest
{
    public class RationalNumber
    {
        public static RationalNumber Zero = new RationalNumber(0);
        public static RationalNumber One = new RationalNumber(1);

        public BigInteger n { get; private set; }
        public BigInteger d { get; private set; }

        public RationalNumber(BigInteger n_, BigInteger d_)
        {
            if (d_ == 0)
            {
                throw new Exception("division by 0");
            }


            var t = gcd(Abs(n_), Abs(d_));
            n = n_ / t;
            d = d_ / t;

            if (d < 0)
            {
                d = -d;
                n = -n;
            }
        }

        public RationalNumber(long n_, long d_ = 1)
            : this(new BigInteger(n_), new BigInteger(d_))
        {
        }

        public static implicit operator RationalNumber(long x)
        {
            return new RationalNumber(x, 1);
        }

        public static implicit operator RationalNumber(BigInteger x)
        {
            return new RationalNumber(x, 1);
        }

        public static RationalNumber operator +(RationalNumber a, RationalNumber b)
        {
            return new RationalNumber(
                a.n * b.d + b.n * a.d,
                a.d * b.d);
        }

        public static RationalNumber operator -(RationalNumber a)
        {
            return new RationalNumber(-a.n, a.d);
        }

        public static RationalNumber operator -(RationalNumber a, RationalNumber b)
        {
            return a + (-b);
        }

        public static RationalNumber operator *(RationalNumber a, RationalNumber b)
        {
            return new RationalNumber(a.n * b.n, a.d * b.d);
        }

        public static RationalNumber operator /(RationalNumber a, RationalNumber b)
        {
            return new RationalNumber(a.n * b.d, a.d * b.n);
        }

        public static bool operator <(RationalNumber a, RationalNumber b)
        {
            return a.n * b.d < a.d * b.n;
        }

        public static bool operator <=(RationalNumber a, RationalNumber b)
        {
            return a.n * b.d <= a.d * b.n;
        }

        public static bool operator >(RationalNumber a, RationalNumber b)
        {
            return a.n * b.d > a.d * b.n;
        }

        public static bool operator >=(RationalNumber a, RationalNumber b)
        {
            return a.n * b.d >= a.d * b.n;
        }

        public override bool Equals(object obj)
        {
            var other = obj as RationalNumber;
            if (obj == null)
            {
                return false;
            }
            return this.n == other.n && this.d == other.d;
        }

        public override int GetHashCode()
        {
            return n.GetHashCode() ^ d.GetHashCode();
        }

        public RationalNumber Sqrt()
        {
            var absn = n >= 0 ? n : -n;
            var sqrtn = Math.Sqrt((double)absn);
            var sqrtd = Math.Sqrt((double)d);
            if (Math.Floor(sqrtn) != sqrtn ||
                Math.Floor(sqrtd) != sqrtd)
            {
                return null;
            }

            return new RationalNumber((int)sqrtn, (int)sqrtd);
        }

        public double AsDouble()
        {
            return (double)n / (double)d;
        }

        public override string ToString()
        {
            if (d == 1)
            {
                return n.ToString();
            }

            return n.ToString() + "/" + d.ToString();
        }

        public static BigInteger gcd(BigInteger x, BigInteger y)
        {
            if (x < y)
            {
                return gcd(y, x);
            }

            while (y != 0)
            {
                var t = y;
                y = x % y;
                x = t;
            }

            return x;
        }

        public static RationalNumber Parse(string str)
        {
            var s = str.Split('/');
            if (s.Length == 1)
            {
                return new RationalNumber(BigInteger.Parse(s[0]), 1);
            }
            else
            {
                return new RationalNumber(BigInteger.Parse(s[0]), BigInteger.Parse(s[1]));
            }
        }

        public static RationalNumber Random(Random r)
        {
            var d = r.Next(1, 5);
            var n = r.Next(0, d + 1);
            return new RationalNumber(n, d);
        }

        public RationalNumber Abs()
        {
            if (n >= 0)
            {
                return this;
            }

            return new RationalNumber(-n, d);
        }

        private static BigInteger Abs(BigInteger x)
        {
            if (x.Sign >= 0)
            {
                return x;
            }

            return -x;
        }
    }
}