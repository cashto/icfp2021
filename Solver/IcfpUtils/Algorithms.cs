using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace IcfpUtils
{
	public struct NoState
	{
	}

	public class NoMove
	{
	}

	public class PredicateComparer<T> : IComparer<T>
	{
		public PredicateComparer(Func<T, T, bool> lessFn)
		{
			this.lessFn = lessFn;
		}

		public int Compare(T x, T y)
		{
			return
				lessFn(x, y) ? 1 :
				lessFn(y, x) ? -1 :
				0;
		}

		private readonly Func<T, T, bool> lessFn;
	}

	public class PriorityQueue<T> : ISearchContainer<T>
	{
		public PriorityQueue(Func<T, T, bool> lessFn)
		{
			this.lessFn = lessFn;
			this.items = new List<T>();
		}

		public int Count => this.items.Count;

		public bool Any() => items.Any();

		public void Push(T obj)
		{
			items.Add(obj);
			var i = items.Count - 1;

			while (lessFn(items[i / 2], items[i]))
			{
				var j = i / 2;
				Swap(i, j);
				i = j;
			}
		}

		public T Pop()
		{
			var i = 0;
			Swap(i, items.Count - 1);

			T obj = items.Last();
			items.RemoveAt(items.Count - 1);

			while (true)
			{
				var largest = i;

				if (i * 2 < items.Count && lessFn(items[largest], items[i * 2]))
				{
					largest = i * 2;
				}

				if (i * 2 + 1 < items.Count && lessFn(items[largest], items[i * 2 + 1]))
				{
					largest = i * 2 + 1;
				}

				if (i == largest)
				{
					return obj;
				}

				Swap(i, largest);

				i = largest;
			}
		}

		public T Peek() => items[0];

		private void Swap(int i, int j)
		{
			T t = items[i];
			items[i] = items[j];
			items[j] = t;
		}

		private readonly Func<T, T, bool> lessFn;
		private readonly List<T> items;
	}

	public interface ISearchContainer<T>
	{
		bool Any();
		void Push(T item);
		T Pop();
		T Peek();
	}

	public class StackSearchContainer<T> : Stack<T>, ISearchContainer<T>
	{
		public bool Any() => ((IEnumerable<T>)this).Any();
	}

	public class QueueSearchContainer<T> : Queue<T>, ISearchContainer<T>
	{
		public bool Any() => ((IEnumerable<T>)this).Any();

		public void Push(T item) => this.Enqueue(item);

		public T Pop() => this.Dequeue();
	}

	public class BoundedPriorityQueue<T> : ISearchContainer<T>
	{
		public BoundedPriorityQueue(
			Func<T, T, bool> lessFn,
			int? maxItems = null,
			Func<T, T, bool> equalFn = null)
		{
			this.items = new SortedDictionary<T, List<T>>(new PredicateComparer<T>(lessFn));
			this.lessFn = lessFn;
			this.equalFn = equalFn;
			this.maxItems = maxItems;
		}

		public bool Any() => items.Any();

		public T Peek()
		{
			if (itemCount == 0)
			{
				throw new InvalidOperationException("Collection is empty");
			}

			var bucket = items.First().Value;
			return bucket[bucket.Count - 1];
		}

		public T Pop()
		{
			var result = Peek();
			PopBucket(items.First());
			return result;
		}

		public void Push(T item)
		{
			if (maxItems.HasValue && 
				itemCount >= maxItems.Value &&
				lessFn(item, worstItem))
			{
				return;
			}

			if (!items.TryGetValue(item, out List<T> bucket))
			{
				bucket = new List<T>();
				items.Add(item, bucket);
			}

			if (equalFn != null && bucket.Any(i => equalFn(i, item)))
			{
				return;
			}

			bucket.Add(item);
			++itemCount;

			if (maxItems.HasValue && itemCount > maxItems.Value)
			{
				PopBucket(items.Last());
			}

			worstItem = items.Last().Key;
		}

		private void PopBucket(KeyValuePair<T, List<T>> pair)
		{
			pair.Value.RemoveAt(pair.Value.Count - 1);
			if (!pair.Value.Any())
			{
				items.Remove(pair.Key);
			}

			--itemCount;
		}

		private readonly SortedDictionary<T, List<T>> items;
		private readonly Func<T, T, bool> lessFn;
		private readonly Func<T, T, bool> equalFn;
		private readonly int? maxItems;
		private int itemCount = 0;
		private T worstItem;
	}

	public class SearchNode<S, M>
	{
		public S State { get; set; }
		public M Move { get; set; }
		public int Depth { get; private set; }
		public SearchNode<S, M> PreviousState { get; set; }

		public SearchNode<S, M> Create(S state, M move)
		{
			return new SearchNode<S, M>()
			{
				State = state,
				Move = move,
				PreviousState = this,
				Depth = Depth + 1
			};
		}

		public IEnumerable<M> Moves
		{
			get
			{
				return GetReverseSearchNodes()
					.Reverse()
					.Skip(1)
					.Select(i => i.Move);
			}
		}

		public IEnumerable<S> States
		{
			get
			{
				return GetReverseSearchNodes()
					.Reverse()
					.Skip(1)
					.Select(i => i.State);
			}
		}

		private IEnumerable<SearchNode<S, M>> GetReverseSearchNodes()
		{
			var node = this;
			while (node != null)
			{
				yield return node;
				node = node.PreviousState;
			}
		}
	}

	public struct SearchNodeEnumerator<S, M>
	{
		public SearchNodeEnumerator(
			SearchNode<S, M> searchNode,
			IEnumerator<SearchNode<S, M>> enumerator)
		{
			SearchNode = searchNode;
			Enumerator = enumerator;
		}

		public SearchNode<S, M> SearchNode { get; private set; }

		public IEnumerator<SearchNode<S, M>> Enumerator { get; private set; }
	}

	public class DepthFirstSearch<S, M> : StackSearchContainer<SearchNodeEnumerator<S, M>>
	{
	}

	public class BreadthFirstSearch<S, M> : QueueSearchContainer<SearchNodeEnumerator<S, M>>
	{
	}

	public class BoundedBreadthFirstSearch<S, M> : ISearchContainer<SearchNodeEnumerator<S, M>>
	{
		public BoundedBreadthFirstSearch(
			Func<SearchNode<S, M>, SearchNode<S, M>, bool> lessFn,
			int? maxItems = null,
			Func<SearchNode<S, M>, SearchNode<S, M>, bool> equalFn = null)
		{
			this.items = new SortedDictionary<int, BoundedPriorityQueue<SearchNodeEnumerator<S, M>>>();
			this.maxItems = maxItems;
			this.lessFn = (lhs, rhs) => lessFn(lhs.SearchNode, rhs.SearchNode);
			if (equalFn != null)
			{
				this.equalFn = (lhs, rhs) => equalFn(lhs.SearchNode, rhs.SearchNode);
			}
		}

		public bool Any() => items.Any();

		public SearchNodeEnumerator<S, M> Peek()
		{
			if (!items.Any())
			{
				throw new InvalidOperationException("Collection is empty");
			}

			return items.First().Value.Peek();
		}

		public SearchNodeEnumerator<S, M> Pop()
		{
			var result = Peek();

			items.First().Value.Pop();
			if (!items.First().Value.Any())
			{
				items.Remove(items.First().Key);
			}

			return result;
		}

		public void Push(SearchNodeEnumerator<S, M> item)
		{
			if (!items.TryGetValue(item.SearchNode.Depth, out BoundedPriorityQueue<SearchNodeEnumerator<S, M>> bucket))
			{
				bucket = new BoundedPriorityQueue<SearchNodeEnumerator<S, M>>(lessFn, maxItems, equalFn);
				items.Add(item.SearchNode.Depth, bucket);
			}

			bucket.Push(item);
		}

		private readonly SortedDictionary<int, BoundedPriorityQueue<SearchNodeEnumerator<S, M>>> items;
		private readonly Func<SearchNodeEnumerator<S, M>, SearchNodeEnumerator<S, M>, bool> lessFn;
		private readonly Func<SearchNodeEnumerator<S, M>, SearchNodeEnumerator<S, M>, bool> equalFn;
		private readonly int? maxItems;
	}
	
	public class BestFirstSearch
	{
		public static ISearchContainer<SearchNodeEnumerator<S, M>> Create<S, M>(
			Func<SearchNode<S, M>, SearchNode<S, M>, bool> lessFn,
			int? maxItems = null)
		{
			if (maxItems.HasValue)
			{
				return new BoundedPriorityQueue<SearchNodeEnumerator<S, M>>(
					(lhs, rhs) => lessFn(lhs.SearchNode, rhs.SearchNode),
					maxItems);
			}
			else
			{
				return new PriorityQueue<SearchNodeEnumerator<S, M>>(
					(lhs, rhs) => lessFn(lhs.SearchNode, rhs.SearchNode));
			}
		}
	}

	public class Algorithims
	{
		/// <summary>
		/// Enumerates over the search space defined by <paramref name="generateMoves"/>.
		/// </summary>
		/// <param name="originalState">The root node state.</param>
		/// <param name="searchContainer">A DepthFirstSearch, BreadthFirstSearch, or a BestFirstSearch object.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <param name="generateMoves">A generator function which takes an existing state and generates a list of states 
		/// that are reachable from it in one move. Each new state should be created via currentState.Create.</param>
		/// <returns></returns>
		public static IEnumerable<SearchNode<State, Move>> Search<State, Move>(
			State originalState,
			ISearchContainer<SearchNodeEnumerator<State, Move>> searchContainer,
			CancellationToken cancellationToken,
			Func<SearchNode<State, Move>, IEnumerable<SearchNode<State, Move>>> generateMoves)
		where Move : class
		{
			var root = new SearchNode<State, Move>()
			{
				State = originalState
			};

			searchContainer.Push(new SearchNodeEnumerator<State, Move>(root, generateMoves(root).GetEnumerator()));

			while (!cancellationToken.IsCancellationRequested && searchContainer.Any())
			{
				var enumerator = searchContainer.Peek().Enumerator;
				if (enumerator.MoveNext())
				{
					var state = enumerator.Current;
					yield return state;
					searchContainer.Push(new SearchNodeEnumerator<State, Move>(state, generateMoves(state).GetEnumerator()));
				}
				else
				{
					searchContainer.Pop();
				}
			}
		}

		public static SearchNode<State, Move> Minmax<State, Move>(
			State originalState,
			Func<State, State, bool> lessFn,
			Func<SearchNode<State, Move>, IEnumerable<SearchNode<State, Move>>> generateMoves,
			bool maximize = true)
		where State : class
		{
			return MinmaxImpl(
				new SearchNode<State, Move>()
				{
					State = originalState
				},
				lessFn,
				generateMoves,
				maximize,
				null,
				null);
		}

		private static SearchNode<State, Move> MinmaxImpl<State, Move>(
			SearchNode<State, Move> node,
			Func<State, State, bool> lessFn,
			Func<SearchNode<State, Move>, IEnumerable<SearchNode<State, Move>>> generateMoves,
			bool maximize,
			State alpha,
			State beta)
		where State : class
		{
			var moves = generateMoves(node).GetEnumerator();
			if (!moves.MoveNext())
			{
				return node;
			}

			var result = MinmaxImpl(moves.Current, lessFn, generateMoves, !maximize, alpha, beta);
			while (moves.MoveNext())
			{
				if (maximize && (alpha == null || lessFn(alpha, result.State)))
				{
					alpha = result.State;
				}

				if (!maximize && (beta == null || lessFn(result.State, beta)))
				{
					beta = result.State;
				}

				if (alpha != null && 
					beta != null && 
					!lessFn(alpha, beta))
				{
					break;
				}

				var candidate = MinmaxImpl(moves.Current, lessFn, generateMoves, !maximize, alpha, beta);
				if (maximize ? lessFn(result.State, candidate.State) : lessFn(candidate.State, result.State))
				{
					result = candidate;
				}
			}

			return result;
		}
	}
}