using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity.InterceptionExtension;

namespace AOP
{
    public class LoggingInterceptionBehavior : IInterceptionBehavior
    {
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logError;
        private readonly Func<object, string> _serializeFunction;

        public LoggingInterceptionBehavior(Action<string> logInfo, Action<string> logError,
            Func<object, string> serializeFunction)
        {
            _logInfo = logInfo;
            _logError = logError;
            _serializeFunction = serializeFunction;
        }

        public IMethodReturn Invoke(IMethodInvocation input,
            GetNextInterceptionBehaviorDelegate getNext)
        {
            LogBefore(input);

            var result = getNext()(input, getNext);

            if (result.Exception != null)
            {
                LogException(input, result.Exception);
            }
            else
            {
                LogAfter(input, result.ReturnValue);
            }

            return result;
        }

        public IEnumerable<Type> GetRequiredInterfaces()
        {
            return Type.EmptyTypes;
        }

        public bool WillExecute => true;

        private string GetStringValue(object obj)
        {
            if (obj == null)
            {
                return "null";
            }

            if (obj.GetType().IsPrimitive || obj.GetType().IsEnum || obj is string)
            {
                return obj.ToString();
            }

            try
            {
                return _serializeFunction?.Invoke(obj) ?? obj.ToString();
            }
            catch
            {
                return obj.ToString();
            }
        }

        private void LogBefore(IMethodInvocation input)
        {
            try
            {
                var beforeMessage = new StringBuilder();
                beforeMessage.AppendLine($"Class  {input.Target.GetType().FullName}");
                beforeMessage.AppendLine($"Method {input.MethodBase.Name} executing");
                var inputParameters = input.MethodBase.GetParameters()
                    .Where(p => p.IsIn || p.IsRetval)
                    .ToList();
                if (inputParameters.Count > 0)
                {
                    beforeMessage.AppendLine("Input Parameters:");
                    foreach (var param in inputParameters)
                    {
                        beforeMessage.AppendLine($"{param.Name}: {GetStringValue(input.Inputs[param.Name])}");
                    }
                }

                _logInfo?.Invoke(beforeMessage.ToString());
            }
            catch (Exception)
            {
                // ignored
                //Do not stop method execution if exception
            }
        }

        private void LogException(IMethodInvocation input, Exception exception)
        {
            try
            {
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"Class {input.Target.GetType().FullName}");
                errorMessage.AppendLine($"Method {input.MethodBase.Name} threw exception");
                //ToDo
                errorMessage.AppendLine(exception.Message);

                _logError?.Invoke(errorMessage.ToString());
            }
            catch (Exception)
            {
                // ignored
                //Method should return original exception
            }
        }

        private void LogAfter(IMethodInvocation input, object result)
        {
            try
            {
                var afterMessage = new StringBuilder();
                afterMessage.AppendLine($"Class {input.Target.GetType().FullName}");
                afterMessage.AppendLine($"Method {input.MethodBase.Name} executed");
                afterMessage.AppendLine("Output:");
                afterMessage.AppendLine(GetStringValue(result));
                var outputParameters = input.MethodBase.GetParameters()
                    .Where(p => p.IsOut || p.IsRetval)
                    .ToList();
                if (outputParameters.Count > 0)
                {
                    afterMessage.AppendLine("Output Parameters:");
                    foreach (var param in outputParameters)
                    {
                        afterMessage.AppendLine($"{param.Name}: {GetStringValue(input.Arguments[param.Name])}");
                    }
                }

                _logInfo?.Invoke(afterMessage.ToString());
            }
            catch (Exception)
            {
                // ignored
                //Do not stop method execution if exception
            }
        }
    }
}