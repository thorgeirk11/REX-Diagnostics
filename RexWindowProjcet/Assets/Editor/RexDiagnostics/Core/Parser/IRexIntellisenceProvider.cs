using Rex.Utilities.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rex.Utilities
{
	interface IRexIntellisenseProvider
	{
		IEnumerable<CodeCompletion> Intellisense(string expression);
	}
}
