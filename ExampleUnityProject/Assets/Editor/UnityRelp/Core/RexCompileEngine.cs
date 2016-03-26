using Rex.Utilities;
using Rex.Utilities.Helpers;
using Rex.Utilities.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

static class RexCompileEngine
{
    private static Thread _compileThread;
    private static bool _runCompilerThread;

    /// <summary>
    /// The continues compiled result by the <see cref="_compileThread"/>.
    /// </summary>
    private static volatile CompiledExpression _currentCompiledExpression;
    public static readonly object CompilerLockObject = new object();

    public static void SetupHelper()
    {
        RexUtils.LoadNamespaceInfos(includeIngoredUsings: false);
        if (_compileThread == null)
        {
            _runCompilerThread = true;
            _compileThread = new Thread(CompilerMainThread);
            _compileThread.Start();
            _compileThread.Name = "REX Compiler thread";
        }
    }

    private static void CompilerMainThread()
    {
        UnityEngine.Debug.Log("Running Compiler thread!");
        var lastCode = "";
        CompileJob lastJob = null;
        Thread lastThread = null;
        var activeThreads = new List<Thread>();
        while (_runCompilerThread)
        {
            activeThreads.RemoveAll(i => (i.ThreadState & System.Threading.ThreadState.Stopped) != 0);

            Thread.Sleep(1);
            if (lastCode != ISM.Code)
            {
                lastCode = ISM.Code;
                if (lastJob != null)
                {
                    lastJob.RequestStop();
                    lastThread.Abort();
                }
                lastJob = new CompileJob();
                lastThread = new Thread(lastJob.CompileCode);
                lastThread.Start(lastCode);
                activeThreads.Add(lastThread);
            }
        }
    }

    internal static CompiledExpression GetCompile(string code)
    {
        var startedWaiting = DateTime.Now;
        while (true)
        {
            lock (CompilerLockObject)
            {
                if (_currentCompiledExpression != null &&
                    _currentCompiledExpression.Parse.WholeCode == code)
                {
                    if (_currentCompiledExpression.Errors.Count > 0)
                    {
                        RexHelper.Messages[MsgType.Error].AddRange(_currentCompiledExpression.Errors);
                        return null;
                    }

                    return _currentCompiledExpression;
                }
            }
            Thread.Sleep(10);
            if (DateTime.Now - startedWaiting > TimeSpan.FromSeconds(2))
            {
                RexHelper.Messages[MsgType.Error].Add("Time out on compiling expression, " + code);
                return null;
            }
        }
    }

    private class CompileJob
    {
        public void CompileCode(object code)
        {
            var parseResult = RexHelper.ParseAssigment((string)code);
            var result = RexHelper.Compile(parseResult);
            lock (CompilerLockObject)
            {
                if (!_shouldStop)
                {
                    UnityEngine.Debug.Log("Done Compiling: " + code);
                    _currentCompiledExpression = result;
                }
            }
        }

        public void RequestStop()
        {
            _shouldStop = true;
        }

        private volatile bool _shouldStop;
    }
}
