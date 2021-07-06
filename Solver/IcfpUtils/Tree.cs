using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace IcfpUtils
{
    public class Tree<T> : IEnumerable<Tree<T>> where T : class
    {
        private List<Tree<T>> children = new List<Tree<T>>();

        public Tree()
        {
        }

        public Tree(IEnumerable<Tree<T>> children)
        {
            this.children = children.ToList();
        }

        public Tree(T value)
        {
            Value = value;
        }

        public bool IsLeaf => !this.children.Any();

        public T Value { get; set; }

        IEnumerator IEnumerable.GetEnumerator() => children.GetEnumerator();

        public IEnumerator<Tree<T>> GetEnumerator() => children.GetEnumerator();

        public Tree<T> this[int index] { get { return children[index]; } }

        public void Add(Tree<T> node)
        {
            this.children.Add(node);
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int indent = 0)
        {
            var sb = new StringBuilder();
            ToStringInternal(sb, 0, indent);
            return sb.ToString();
        }

        private void ToStringInternal(
            StringBuilder sb,
            int depth,
            int indent)
        {
            if (IsLeaf)
            {
                sb.Append(Value);
            }
            else
            {
                if (indent > 0)
                {
                    sb.Append(Environment.NewLine);
                }

                foreach (var i in Enumerable.Range(0, indent * depth))
                {
                    sb.Append(' ');
                }

                sb.Append('(');

                var firstChild = true;
                foreach (var child in this)
                {
                    if (!firstChild)
                    {
                        sb.Append(' ');
                    }

                    child.ToStringInternal(sb, depth + 1, indent);

                    firstChild = false;
                } 

                sb.Append(')');
            }
        }

        public IEnumerable<IEnumerable<Tree<T>>> Walk()
        {
            var noMove = new NoMove();

            var ans = Algorithims.Search(
                this,
                new DepthFirstSearch<Tree<T>, NoMove>(),
                CancellationToken.None,
                (state) => state.State.Select(i => state.Create(i, noMove)));

            return ans.Select(i => i.States);
        }

        public Tree<U> Map<U>(Func<T, U> fn) where U : class
        {
            var ans = new Tree<U>(fn(Value));
            ans.children.AddRange(children.Select(i => i.Map(fn)));
            return ans;
        }
    }
}