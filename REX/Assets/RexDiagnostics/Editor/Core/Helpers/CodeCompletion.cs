namespace Rex.Utilities.Helpers
{
	public class CodeCompletion
	{
		public string SyntaxHighlightedDetails { get; set; }
		public MemberDetails Details { get; set; }
		public int Start { get; set; }
		public int End { get; set; }
		public string ReplaceString { get; set; }

		public bool IsMethodOverload { get; set; }

		/// <summary>
		/// Is this entry in scope. Is it in the using statments?
		/// </summary>
		public bool IsInScope { get; set; }

		public string Search { get; set; }
		public override string ToString()
		{
			return Details.ToString();
		}
	}
}
