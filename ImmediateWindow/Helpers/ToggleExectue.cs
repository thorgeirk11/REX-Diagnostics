using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rex.Utilities.Helpers
{
    public class ToggleExecution<T>
       where T : AConsoleOutput, new()
    {
        public CompiledExpression Compile { get; set; }
        public bool KeepGoing { get; set; }
        public ToggleType Type { get; set; }
        public bool SelectedAsOutput { get; set; }
        public int MaxExecuteCount { get; set; }
        public int CurrentExecuteCount { get; set; }

        public T Result { get; set; }
        public object yeildWait { get; set; }
    }
    public class CompiledExpression
    {
        public Assembly Assembly { get; set; }
        public ParseResult Parse { get; set; }

        public FuncType FuncType { get; set; }
    }
    public class ParseResult
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

    public enum ToggleType
    {
        Once = -1,
        OnceAFrame = 0,
        OnceASec = 1,
        EveryFiveSec = 5,
        EveryTenSec = 10,
    }

    public enum FuncType { _object, _void }

}
