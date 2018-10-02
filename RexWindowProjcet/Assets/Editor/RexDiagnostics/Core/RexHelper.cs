using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rex.Utilities.Helpers;
using Rex.Window;
using UnityEditor;

namespace Rex.Utilities
{
    public static class RexHelper
    {
        public class Variable
        {
            public object VarValue { get; set; }
            public Type VarType { get; set; }
        }
        public static readonly Dictionary<string, Variable> Variables = new Dictionary<string, Variable>();

        public static IEnumerable<OutputEntry> Output { get { return _outputList; } }

        private static readonly LinkedList<OutputEntry> _outputList = new LinkedList<OutputEntry>();

        const int OUTPUT_LENGHT = 20;

        /// <summary>
        /// Executes the expression and retunrs a output entry with the results.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="AOutputEntry"/></typeparam>
        /// <param name="compileResult">compiled code to execute.</param>
        /// <param name="messages">Any errors or warnings are added to this dic.</param>
        public static T Execute<T>(CompiledExpression compileResult, out Dictionary<MessageType, List<string>> messages)
            where T : IOutputEntry, new()
        {
            messages = new Dictionary<MessageType, List<string>>();
            try
            {
                var value = Invoke(compileResult);

                // If this is a variable declaration
                if (compileResult.Parse.IsDeclaring)
                {
                    DeclaringVariable(compileResult.Parse.Variable, value, messages);
                }

                var output = new T();
                if (compileResult.FuncType == FuncType._void)
                {
                    output.LoadVoid();
                }
                else
                {
                    output.LoadObject(value);
                }
                return output;
            }
            catch (Exception ex)
            {
                var exception = ex.InnerException == null ? ex : ex.InnerException;

                if (exception is AccessingDeletedVariableException)
                {
                    var deletedVar = exception as AccessingDeletedVariableException;
                    var msg = "Variable " + deletedVar.VarName + " has been deleted, but is still being accessed";

                    messages.Add(MessageType.Warning, msg);
                    return new T
                    {
                        Exception = deletedVar
                    };
                }

                messages.Add(MessageType.Error, exception.ToString());
                return new T { Exception = exception };
            }
        }

        /// <summary>
        /// Invoke the delegate containg the user defined code.
        /// </summary>
        /// <param name="compileResult">Compile result to invoke.</param>
        private static object Invoke(CompiledExpression compileResult)
        {
            if (compileResult.FuncType == FuncType._object)
            {
                if (!compileResult.HasInitialized)
                {
                    compileResult.InitializedFunction = ExecuteAssembly<Func<object>>(compileResult.Assembly);
                }
                return compileResult.InitializedFunction();
            }
            else
            {
                if (!compileResult.HasInitialized)
                {
                    compileResult.InitializedAction = ExecuteAssembly<Action>(compileResult.Assembly);
                }
                compileResult.InitializedAction();
                return null;
            }
        }

        /// <summary>
        /// Invokes <see cref="RexCompileEngine.REX_FUNC_NAME"/> inside the compiled assembly. 
        /// </summary>
        /// <typeparam name="T">Return type of the function.</typeparam>
        /// <param name="assembly">Assembly containing the commpiled code.</param>
        private static T ExecuteAssembly<T>(Assembly assembly) where T : class
        {
            var Class = Activator.CreateInstance(assembly.GetType(RexCompileEngine.REX_CLASS_NAME));
            var method = Class.GetType().GetMethod(RexCompileEngine.REX_FUNC_NAME);
            return method.Invoke(Class, null) as T;
        }


        /// <summary>
        /// Handles a variable declaration.
        /// </summary>
        /// <param name="varName">Name of the variable</param>
        /// <param name="val">Value of the variable</param>
        /// <param name="showMessages">Should show an warning message or not</param>
        /// <param name="messages">Any errors or warnings are added to this dic.</param>
        private static void DeclaringVariable(string varName, object val, Dictionary<MessageType, List<string>> messages)
        {
            var warning = string.Empty;
            if (val != null)
            {
                var valType = val.GetType();

                if (RexReflectionUtils.ContainsAnonymousType(valType))
                {
                    warning = string.Format("Cannot declare a variable '{0}' with anonymous type", varName);
                }
                else
                {
                    if (!valType.IsVisible)
                    {
                        var interfaces = valType.GetInterfaces();
                        var iEnumerable = interfaces.FirstOrDefault(t => t.IsGenericType && t.GetInterface("IEnumerable") != null);
                        if (iEnumerable != null)
                        {
                            Variables[varName] = new Variable { VarValue = val, VarType = iEnumerable };
                            return;
                        }
                        warning = string.Format("Expression returned a compiler generated class. Could not declare variable '{0}'", varName);
                    }
                    else
                    {
                        Variables[varName] = new Variable { VarValue = val, VarType = valType };
                        return;
                    }
                }
            }
            else
            {
                warning = string.Format("Expression returned null. Could not declare variable '{0}'", varName);
            }
            messages.Add(MessageType.Warning, warning);
        }

        /// <summary>
        /// Outputs errors if there are any. returns true if there are none.
        /// </summary>
        /// <param name="result">todo: describe result parameter on DealWithErrors</param>
        public static IEnumerable<string> DealWithErrors(CompilerResults result)
        {
            var errorList = new List<string>();
            foreach (CompilerError error in result.Errors)
            {
                if (error.IsWarning) continue;

                // CS0266: Cannot implicitly convert type '' to ''.An explicit conversion exists (are you missing a cast?) 
                // CS0246: The type or namespace name '' could not be found (are you missing a using directive or an assembly reference?)
                // CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                if (errorList.Count > 0 && (error.ErrorNumber == "CS0266" ||
                                            error.ErrorNumber == "CS0246" ||
                                            error.ErrorNumber == "CS0201"))
                {
                    continue;
                }

                if (!errorList.Contains(error.ErrorText))
                {
                    errorList.Add(error.ErrorText);
                }
            }
            return errorList;
        }

        public static void RemoveOutput(OutputEntry deleted)
        {
            _outputList.Remove(deleted);
        }

        public static void AddOutput(OutputEntry output)
        {
            if (output == null)
                return;

            foreach (var item in _outputList)
            {
                item.ShowDetails = false;
                item.ShowEnumeration = false;
            }

            output.ShowDetails = true;
            output.ShowEnumeration = true;
            if (_outputList.Count >= OUTPUT_LENGHT)
            {
                _outputList.RemoveLast();
            }
            _outputList.AddFirst(output);
        }

        public static void ClearOutput()
        {
            _outputList.Clear();
        }
    }
}
