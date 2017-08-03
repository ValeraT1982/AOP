using System;
using System.Text;

namespace AOP
{
    public static class Extensions
    {
        public static string GetDescription(this Exception e)
        {
            var builder = new StringBuilder();

            AddException(builder, e);

            return builder.ToString();
        }

        private static void AddException(StringBuilder builder, Exception e)
        {
            builder.AppendLine($"Message: {e.Message}");
            builder.AppendLine($"Stack Trace: {e.StackTrace}");

            if (e.InnerException != null)
            {
                builder.AppendLine("Inner Exception");
                AddException(builder, e.InnerException);
            }
        }
    }
}
