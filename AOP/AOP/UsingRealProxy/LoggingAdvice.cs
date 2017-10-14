#if NET461
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Threading.Tasks;

namespace AOP.UsingRealProxy
{
    public class LoggingAdvice<T> : RealProxy
    {
        private readonly T _decorated;
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logError;
        private readonly Func<object, string> _serializeFunction;
        private readonly TaskScheduler _loggingScheduler;

        private LoggingAdvice(T decorated, Action<string> logInfo, Action<string> logError,
            Func<object, string> serializeFunction, TaskScheduler loggingScheduler)
            : base(typeof(T))
        {
            if (decorated == null)
            {
                throw new ArgumentNullException(nameof(decorated));
            }

            _decorated = decorated;
            _logInfo = logInfo;
            _logError = logError;
            _serializeFunction = serializeFunction;
            _loggingScheduler = loggingScheduler ?? TaskScheduler.FromCurrentSynchronizationContext();
        }

        public static T Create(T decorated, Action<string> logInfo, Action<string> logError,
            Func<object, string> serializeFunction, TaskScheduler loggingScheduler = null)
        {
            var advice = new LoggingAdvice<T>(decorated, logInfo, logError, serializeFunction, loggingScheduler);

            return (T)advice.GetTransparentProxy();
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;
            if (methodCall != null)
            {
                var methodInfo = methodCall.MethodBase as MethodInfo;
                if (methodInfo != null)
                {
                    try
                    {
                        try
                        {
                            LogBefore(methodCall, methodInfo);
                        }
                        catch (Exception ex)
                        {
                            //Do not stop method execution if exception
                            LogException(ex);
                        }

                        var args = methodCall.Args;

                        var result = typeof(T).InvokeMember(
                            methodCall.MethodName,
                            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, _decorated, args);

                        if (result is Task)
                        {
                            ((Task)result).ContinueWith(task =>
                           {
                               if (task.Exception != null)
                               {
                                   LogException(task.Exception.InnerException ?? task.Exception, methodCall);
                               }
                               else
                               {
                                   object taskResult = null;

                                   if (task.GetType().IsGenericType && task.GetType().GetGenericTypeDefinition() == typeof(Task<>))
                                   {
                                       var property = task.GetType().GetProperties()
                                           .FirstOrDefault(p => p.Name == "Result");

                                       if (property != null)
                                       {
                                           taskResult = property.GetValue(task);
                                       }
                                   }

                                   LogAfter(methodCall, methodCall.Args, methodInfo, taskResult);
                               }
                           },
                           _loggingScheduler);
                        }
                        else
                        {
                            try
                            {
                                LogAfter(methodCall, args, methodInfo, result);
                            }
                            catch (Exception ex)
                            {
                                //Do not stop method execution if exception
                                LogException(ex);
                            }
                        }

                        return new ReturnMessage(result, args, args.Length,
                            methodCall.LogicalCallContext, methodCall);
                    }
                    catch (Exception ex)
                    {
                        if (ex is TargetInvocationException)
                        {
                            LogException(ex.InnerException ?? ex, methodCall);

                            return new ReturnMessage(ex.InnerException ?? ex, methodCall);
                        }
                    }
                }
            }

            throw new ArgumentException(nameof(msg));
        }

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

        private void LogException(Exception exception, IMethodCallMessage methodCall = null)
        {
            try
            {
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"Class {_decorated.GetType().FullName}");
                errorMessage.AppendLine($"Method {methodCall?.MethodName} threw exception");
                errorMessage.AppendLine(exception.GetDescription());

                _logError?.Invoke(errorMessage.ToString());
            }
            catch (Exception)
            {
                // ignored
                //Method should return original exception
            }
        }

        private void LogAfter(IMethodCallMessage methodCall, object[] args, MethodInfo methodInfo, object result)
        {
            var afterMessage = new StringBuilder();
            afterMessage.AppendLine($"Class {_decorated.GetType().FullName}");
            afterMessage.AppendLine($"Method {methodCall.MethodName} executed");
            afterMessage.AppendLine("Output:");
            afterMessage.AppendLine(GetStringValue(result));
            var parameters = methodInfo.GetParameters();
            if (parameters.Any())
            {
                afterMessage.AppendLine("Parameters:");
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var arg = args[i];
                    afterMessage.AppendLine($"{parameter.Name}:{GetStringValue(arg)}");
                }
            }

            _logInfo?.Invoke(afterMessage.ToString());
        }

        private void LogBefore(IMethodCallMessage methodCall, MethodInfo methodInfo)
        {
            var beforeMessage = new StringBuilder();
            beforeMessage.AppendLine($"Class {_decorated.GetType().FullName}");
            beforeMessage.AppendLine($"Method {methodCall.MethodName} executing");
            var parameters = methodInfo.GetParameters();
            if (parameters.Any())
            {
                beforeMessage.AppendLine("Parameters:");

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var arg = methodCall.Args[i];
                    beforeMessage.AppendLine($"{parameter.Name}:{GetStringValue(arg)}");
                }
            }

            _logInfo?.Invoke(beforeMessage.ToString());
        }
    }
}
#endif