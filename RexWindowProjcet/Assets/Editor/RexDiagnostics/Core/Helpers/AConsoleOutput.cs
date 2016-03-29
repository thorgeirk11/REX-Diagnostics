using Rex.Utilities.Helpers;
using System;
using System.Collections.Generic;

namespace Rex.Utilities.Helpers
{
    public enum MsgType
    {
        None,
        Info,
        Warning,
        Error
    }
    public abstract class AConsoleOutput
    {
        public virtual Exception Exception { get; set; }
        public string Message { get; set; }
        public bool ShowMembers { get; set; }
        public bool ShowDetails { get; set; }
        public List<AConsoleOutput> Members { get; set; }

        public abstract void LoadInDetails(object value, string message, IEnumerable<MemberDetails> details);

        protected AConsoleOutput()
        {
            Exception = null;
            ShowMembers = false;
            ShowDetails = false;
            Members = new List<AConsoleOutput>();
        }

        public abstract void Display();
    }
}
