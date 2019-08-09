using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rex.Utilities.Helpers
{
	public class CompiledExpression
	{
		public Assembly Assembly { get; set; }
		public ParseResult Parse { get; set; }

		public IEnumerable<string> Errors { get; set; }

		public Func<object> InitializedFunction { get; set; }
		public Action InitializedAction { get; set; }

		public bool HasInitialized
		{
			get { return InitializedFunction != null || InitializedAction != null; }
		}

		public FuncType FuncType { get; set; }
	}
	public struct ParseResult
	{
		public bool IsDeclaring { get; set; }
		public string Variable { get; set; }
		public string TypeString { get; set; }
		public string ExpressionString { get; set; }
		public string WholeCode { get; set; }
	}

	public class HistoryItem
	{
		public CompiledExpression Compile { get; set; }
		public bool IsExpanded { get; set; }
	}

	public enum FuncType { _object, _void }

}
