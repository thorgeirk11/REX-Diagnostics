using Rex.Utilities.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rex.Utilities.Helpers
{
	/// <summary>
	/// Abstract entry for output of an expression.
	/// </summary>
	public abstract class AOutputEntry
	{
		/// <summary>
		/// The exception if the expression throwed one.
		/// </summary>
		public virtual Exception Exception { get; set; }

		/// <summary>
		/// The main output text.
		/// </summary>
		public string Text { get; protected set; }

		protected AOutputEntry()
		{
			Text = string.Empty;
		}

		/// <summary>
		/// Setups an output from a expression with no return (statment).
		/// </summary>
		public virtual void LoadVoid()
		{
			Text = "Expression successfully executed.";
		}

		/// <summary>
		/// Setups an output with the given value.
		/// </summary>
		/// <param name="value">Object returned by the expression</param>
		public void LoadObject(object value)
		{
			if (value != null &&
				!(value is string || value is Enum) &&
				value is IEnumerable)
			{
				LoadEnumeration(value as IEnumerable);
			}
			else
			{
				LoadSingleObject(value);
			}
		}

		/// <summary>
		/// Setups a single value output.
		/// </summary>
		/// <param name="value">output value</param>
		protected virtual void LoadSingleObject(object value)
		{
			if (value == null)
			{
				Text = "null";
				return;
			}

			var valType = value.GetType();
			if (RexUtils.IsToStringOverride(valType))
			{
				Text = value.ToString();
			}
			else
			{
				Text = RexUtils.GetCSharpRepresentation(valType, true).ToString();
			}
		}
		/// <summary>
		/// Setups an output that contains an <see cref="IEnumerable"/> value e.g array/list.
		/// </summary>
		/// <param name="values">Enumeration to dispay in this output entry.</param>
		protected abstract void LoadEnumeration(IEnumerable values);

		/// <summary>
		/// Draws the output entry inside the UI.
		/// </summary>
		public abstract void DrawOutputUI();

		public abstract bool Filter(string text);

	}
}
