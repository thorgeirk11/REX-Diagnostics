using Rex.Utilities.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rex.Utilities
{
	interface IRexIntellisenceProvider
	{
		IEnumerable<CodeCompletion> Intellisence(string exprssion);
	}
}
