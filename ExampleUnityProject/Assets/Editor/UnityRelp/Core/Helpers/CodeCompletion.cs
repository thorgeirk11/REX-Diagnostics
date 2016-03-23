namespace Rex.Utilities.Helpers
{
    public class CodeCompletion
    {
        public MemberDetails Details { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public string ReplaceString { get; set; }

        public bool IsMethodOverload { get; set; }

        public string Search { get; set; }
        public override string ToString()
        {
            return Details.ToString();
        }
    }
}
