using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace IcfpUtils
{
    public static class Utils
    {
        public static List<T> Shuffle<T>(this IEnumerable<T> x)
        {
            var ans = x.ToList();
            var rand = new Random();

            for (var i = 0; i < ans.Count; ++i)
            {
                var j = rand.Next(ans.Count - i) + i;
                var t = ans[i];
                ans[i] = ans[j];
                ans[j] = t;
            }

            return ans;
        }

        public static T Largest<T, U>(this IEnumerable<T> list, Func<T, U> fn) where U : IComparable<U>
        {
            var ans = list.First();
            var ansScore = fn(ans);

            foreach (var item in list.Skip(1))
            {
                var newScore = fn(item);
                if (newScore.CompareTo(ansScore) > 0)
                {
                    ansScore = newScore;
                    ans = item;
                }
            }

            return ans;
        }

        public static T Bound<T>(T x, T min, T max) where T : IComparable<T>
        {
            if (x.CompareTo(min) < 0)
            {
                return min;
            }

            if (x.CompareTo(max) > 0)
            {
                return max;
            }

            return x;
        }

        private static int CharToDigit(char ch)
        {
            return
                ch > 'a' ? 10 + ch - 'a' :
                ch > 'A' ? 10 + ch - 'A' :
                ch - '0';
        }

        private static char DigitToChar(int x)
        {
            return (char)(x < 10 ? '0' + x : 'a' + x - 10);
        }

        public static BigInteger BigIntegerFromString(string s, int radix)
        {
            var ans = new BigInteger(0);
            foreach (var c in s)
            {
                ans = ans * radix + CharToDigit(c);
            }

            return ans;
        }

        public static string ToString(this BigInteger x, int radix)
        {
            if (x == 0)
            {
                return "0";
            }

            var sb = new List<char>();
            while (x != 0)
            {
                sb.Add(DigitToChar((int)(x % radix)));
                x /= radix;
            }

            return new string(sb.Reverse<char>().ToArray());
        }
    }
}
