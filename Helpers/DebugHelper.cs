using System;
using System.Diagnostics;

namespace ARNativePortal.Helpers
{
    public partial class DebugHelper
    {
        public string FunctionName()
        {
            var stack = new StackTrace();
            var stackFrame = stack.GetFrame(1);
            return stackFrame.GetMethod().Name;
        }
    }

    public partial class DebugHelper
    {
        public static readonly DebugHelper Instance = new DebugHelper();

        private DebugHelper() { }
    }
}
