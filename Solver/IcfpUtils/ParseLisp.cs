using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IcfpUtils
{
    public class LispNode
    {
        public string Text { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string LineText { get; set; }

        public override string ToString() => Text;
    }

    public static class Lisp
    {
        private enum LispNodeType
        {
            Token,
            Open,
            Close
        }

        private class LispNodeAndType
        {
            public LispNodeType Type { get; set; }
            public LispNode Node { get; set; }
        }

        private static MemoryStream GetStream(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value));
        }

        public static Tree<LispNode> Parse(string s)
        {
            using (var stream = GetStream(s))
            {
                return Parse(stream);
            }
        }

        public static Tree<LispNode> Parse(Stream stream)
        {
            var stack = new Stack<Tree<LispNode>>();
            stack.Push(new Tree<LispNode>());

            foreach (var token in Tokenize(stream))
            {
                switch (token.Type)
                {
                    case LispNodeType.Open:
                        stack.Push(new Tree<LispNode>(token.Node));
                        break;

                    case LispNodeType.Close:
                        var item = stack.Pop();
                        if (!stack.Any())
                        {
                            throw new Exception($"Mismatched ) at line {token.Node.Line} column {token.Node.Column}");
                        }

                        stack.Peek().Add(item);
                        break;

                    case LispNodeType.Token:
                        stack.Peek().Add(new Tree<LispNode>(token.Node));
                        break;
                }
            }

            var ans = stack.Pop();
            if (stack.Any())
            {
                throw new Exception($"Mismatched ( at line {ans.Value.Line} column {ans.Value.Column}");
            }

            return ans;
        }

        private static IEnumerable<string> ReadLines(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        private static IEnumerable<LispNodeAndType> Tokenize(Stream stream)
        {
            var line = 1;
            foreach (var lineText in ReadLines(stream))
            {
                var escape = false;
                char? quoteChar = null;
                var inComment = false;
                var column = 1;
                var tokenStartColumn = column;
                var token = new StringBuilder();

                foreach (var ch in lineText)
                {
                    if (quoteChar != null)
                    {
                        if (escape)
                        {
                            token.Append(ch);
                            escape = false;
                        }
                        else if (ch == '\\')
                        {
                            escape = true;
                        }
                        else if (ch == quoteChar.Value)
                        {
                            quoteChar = null;

                            yield return new LispNodeAndType()
                            {
                                Type = LispNodeType.Token,
                                Node = new LispNode()
                                {
                                    Line = line,
                                    Column = tokenStartColumn,
                                    LineText = lineText,
                                    Text = token.ToString()
                                }
                            };

                            token = new StringBuilder();
                        }
                        else
                        {
                            token.Append(ch);
                        }
                    }
                    else if (!inComment)
                    {
                        switch (ch)
                        {
                            case '\'':
                            case '\"':
                                quoteChar = ch;
                                tokenStartColumn = column;
                                break;

                            case '#':
                                inComment = true;
                                break;

                            case '(':
                                yield return new LispNodeAndType()
                                {
                                    Type = LispNodeType.Open,
                                    Node = new LispNode()
                                    {
                                        Line = line,
                                        Column = column,
                                        LineText = lineText
                                    }
                                };

                                tokenStartColumn = column + 1;
                                break;

                            case ' ':
                            case '\t':
                            case ')':
                                if (token.Length > 0)
                                {
                                    yield return new LispNodeAndType()
                                    {
                                        Type = LispNodeType.Token,
                                        Node = new LispNode()
                                        {
                                            Line = line,
                                            Column = tokenStartColumn,
                                            LineText = lineText,
                                            Text = token.ToString()
                                        }
                                    };

                                    token = new StringBuilder();
                                }

                                tokenStartColumn = column + 1;

                                if (ch == ')')
                                {
                                    yield return new LispNodeAndType()
                                    {
                                        Type = LispNodeType.Close,
                                        Node = new LispNode()
                                        {
                                            Line = line,
                                            Column = column,
                                            LineText = lineText
                                        }
                                    };
                                }
                                break;

                            default:
                                token.Append(ch);
                                break;
                        }
                    }

                    ++column;
                }

                if (token.Length > 0)
                {
                    yield return new LispNodeAndType()
                    {
                        Type = LispNodeType.Token,
                        Node = new LispNode()
                        {
                            Line = line,
                            Column = tokenStartColumn,
                            LineText = lineText,
                            Text = token.ToString()
                        }
                    };
                }

                ++line;
            }
        }
    }
}