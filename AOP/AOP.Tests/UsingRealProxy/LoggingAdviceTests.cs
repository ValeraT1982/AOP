#if NET461
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AOP.UsingRealProxy;
using NSubstitute;
using NUnit.Framework;

namespace AOP.Tests.UsingRealProxy
{
    public interface ITestClass2
    {
        void Method();

        void MethodWithException();

        Task AsyncMethodWithException();
    }

    public class TestClass2 : ITestClass2
    {
        public void Method()
        {

        }

        public void MethodWithException()
        {
            throw new Exception();
        }

        public Task AsyncMethodWithException()
        {
            return Task.Factory.StartNew(() => { throw new Exception(); });
        }
    }

    [TestFixture]
    public class LoggingAdviceTests
    {
        private TaskScheduler _taskScheduler;

        public class Data
        {
            public string Prop { get; set; }
        }
        
        public interface ITestClass
        {
            string Property { get; set; }

            void MethodWithVoidResult();

            string MethodWithStringResultAndIntParameter(int intParam);
            
            Data MethodWithClassResultAndClassParameter(Data dataParam);

            int MethodWithOutParameter(out string stringParam);

            int MethodWithRefParameter(ref string stringParam);

            int MethodWithOutParameter(out int intParam);

            int MethodWithRefParameter(ref int intParam);

            int MethodWithMixedParameter(ref int refParam, out string outParam, bool param);

            Task MethodWithTaskResult();

            Task<string> MethodWithTaskStringResult();
        }

        [SetUp]
        public void SetUp()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            _taskScheduler = new CurrentThreadTaskScheduler();
        }

        [Test]
        public void ThrowException_WhenDecoratedClassNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var decorated = LoggingAdvice<ITestClass>.Create(
                    null,
                    s => { },
                    s => { },
                    o => o?.ToString());
            });
        }

        [Test]
        public void LogInfo()
        {
            var testClass = Substitute.For<ITestClass>();
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass, 
                s => infoMassages.Add(s), 
                s => errorMassages.Add(s),
                o => o?.ToString());
            
            decorated.MethodWithVoidResult();
            
            testClass.Received().MethodWithVoidResult();
            Assert.AreEqual(2, infoMassages.Count);
            Assert.IsTrue(infoMassages[0].Contains("MethodWithVoidResult"));
            Assert.IsTrue(infoMassages[1].Contains("MethodWithVoidResult"));
            Assert.AreEqual(0, errorMassages.Count);
        }

        [Test]
        public void DoNotStop_WhenLogInfoThrowsException()
        {
            var testClass = Substitute.For<ITestClass>();
            var errorMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => { throw new Exception(); },
                s => errorMassages.Add(s),
                o => o?.ToString());

            decorated.MethodWithVoidResult();

            testClass.Received().MethodWithVoidResult();
            Assert.AreEqual(2, errorMassages.Count);
        }

        [Test]
        public void LogInfo_WhenClassResultAndClassParameter()
        {
            var testClass = Substitute.For<ITestClass>();
            testClass.MethodWithClassResultAndClassParameter(Arg.Any<Data>()).Returns(new Data { Prop = "Result12345" });
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass, 
                s => infoMassages.Add(s), 
                s => errorMassages.Add(s), 
                o => ((Data)o).Prop);
            
            var result = decorated.MethodWithClassResultAndClassParameter(new Data { Prop = "Parameter12345"});
            
            testClass.Received().MethodWithClassResultAndClassParameter(Arg.Any<Data>());
            Assert.AreEqual("Result12345", result.Prop);
            Assert.AreEqual(2, infoMassages.Count);
            Assert.IsTrue(infoMassages[0].Contains("dataParam:Parameter12345"));
            Assert.IsTrue(infoMassages[1].Contains("dataParam:Parameter12345"));
            Assert.IsTrue(infoMassages[1].Contains("Result12345"));
            Assert.AreEqual(0, errorMassages.Count);
        }

        [Test]
        public void LogInfo_WhenParameters()
        {
            var testClass = Substitute.For<ITestClass>();
            testClass.MethodWithStringResultAndIntParameter(Arg.Any<int>()).Returns("Result12345");
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                    testClass, 
                    s => infoMassages.Add(s), 
                    s => errorMassages.Add(s),
                    o => o?.ToString());
            
            var result = decorated.MethodWithStringResultAndIntParameter(12345);
            
            testClass.Received().MethodWithStringResultAndIntParameter(Arg.Any<int>());
            Assert.AreEqual("Result12345", result);
            Assert.AreEqual(2, infoMassages.Count);
            Assert.IsTrue(infoMassages[0].Contains("intParam:12345"));
            Assert.IsTrue(infoMassages[1].Contains("Result12345"));
            Assert.IsTrue(infoMassages[1].Contains("intParam:12345"));
            Assert.AreEqual(0, errorMassages.Count);
        }

        [Test]
        public void DoNotStop_WhenSerializerThrowsException()
        {
            var testClass = Substitute.For<ITestClass>();
            testClass.MethodWithClassResultAndClassParameter(Arg.Any<Data>()).Returns(new Data { Prop = "Result12345" });
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                    testClass, 
                    s => infoMassages.Add(s), 
                    s => errorMassages.Add(s), 
                    o => { throw new Exception();});
            Data result = null;
            
            Assert.DoesNotThrow(() => result = decorated.MethodWithClassResultAndClassParameter(new Data { Prop = "Parameter12345"}));
            testClass.Received().MethodWithClassResultAndClassParameter(Arg.Any<Data>());
            Assert.AreEqual("Result12345", result?.Prop);
            Assert.AreEqual(2, infoMassages.Count);
            Assert.IsTrue(infoMassages[0].Contains("Data"));
            Assert.IsTrue(infoMassages[1].Contains("Data"));
            Assert.AreEqual(0, errorMassages.Count);
        }
        
        [Test]
        public void LogError()
        {
            var testClass = Substitute.For<ITestClass>();
            testClass.When(t => t.MethodWithVoidResult()).Do(info => { throw new Exception(); });
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                    testClass, 
                    s => infoMassages.Add(s), 
                    s => errorMassages.Add(s),
                    o => o?.ToString());
            
            Assert.Throws<Exception>(() => decorated.MethodWithVoidResult());
            testClass.Received().MethodWithVoidResult();
            Assert.AreEqual(1, infoMassages.Count);
            Assert.IsTrue(infoMassages[0].Contains("MethodWithVoidResult"));
            Assert.AreEqual(1, errorMassages.Count);
            Assert.IsTrue(errorMassages[0].Contains("MethodWithVoidResult"));
        }

        [Test]
        public void DoNotStop_WhenLogErrorThrowsException()
        {
            var testClass = Substitute.For<ITestClass>();
            testClass.When(t => t.MethodWithVoidResult()).Do(info => { throw new Exception(); });
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => { throw new ArgumentNullException(); },
                o => o?.ToString());

            Assert.Throws<Exception>(() => decorated.MethodWithVoidResult());

            testClass.Received().MethodWithVoidResult();
            Assert.AreEqual(1, infoMassages.Count);
        }

        [Test]
        public void LogInfo_WhenTaskMethodCall()
        {
            var testClass = Substitute.For<ITestClass>();
            testClass.MethodWithTaskResult().Returns(Task.CompletedTask);
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString(),
                _taskScheduler);

            decorated.MethodWithTaskResult().Wait();
            ReleaseContext();

            testClass.Received().MethodWithTaskResult();
            Assert.AreEqual(2, infoMassages.Count);
            Assert.AreEqual(0, errorMassages.Count);
        }

        [Test]
        public void LogInfo_WhenTaskMethodCallWithException()
        {
            var testClass = Substitute.For<ITestClass>();
            testClass.MethodWithTaskResult().Returns(Task.FromException(new Exception("ERROR!!!!")));
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString(),
                _taskScheduler);

            Assert.Throws<AggregateException>(() => decorated.MethodWithTaskResult().Wait());
            ReleaseContext();

            testClass.Received().MethodWithTaskResult();
            Assert.AreEqual(1, infoMassages.Count);
            Assert.AreEqual(1, errorMassages.Count);
            Assert.IsTrue(errorMassages[0].Contains("ERROR!!!!"));
        }

        [Test]
        public void LogInfo_WhenTaskHasResult()
        {
            var testClass = Substitute.For<ITestClass>();
            testClass.MethodWithTaskStringResult().Returns("String result");
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString(),
                _taskScheduler);

            var result = decorated.MethodWithTaskStringResult().Result;
            ReleaseContext();

            Assert.AreEqual("String result", result);
            testClass.Received().MethodWithTaskStringResult();
            Assert.AreEqual(2, infoMassages.Count);
            Assert.IsTrue(infoMassages[1].Contains("String result"));
            Assert.AreEqual(0, errorMassages.Count);
        }

        [Test]
        public void LogInfo_WhenTaskWithResultThrowException()
        {
            var testClass = Substitute.For<ITestClass>();
            testClass.MethodWithTaskStringResult().Returns(Task.FromException<string>(new Exception("ERROR!!!!")));
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString(),
                _taskScheduler);

            Assert.Throws<AggregateException>(() => decorated.MethodWithTaskStringResult().Wait());
            ReleaseContext();

            testClass.Received().MethodWithTaskStringResult();
            Assert.AreEqual(1, infoMassages.Count);
            Assert.AreEqual(1, errorMassages.Count);
            Assert.IsTrue(errorMassages[0].Contains("ERROR!!!!"));
        }

        [Test]
        public void LogInfo_WhenOutParameter()
        {
            var testClass = Substitute.For<ITestClass>();
            var val = "s2";
            testClass.MethodWithOutParameter(out val)
                .Returns(x =>
                {
                    x[0] = "s5";

                    return 25;
                });

            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString());

            var result = decorated.MethodWithOutParameter(out val);

            var val2 = "s2";
            testClass.Received().MethodWithOutParameter(out val2);
            Assert.AreEqual(0, errorMassages.Count);
            Assert.AreEqual(2, infoMassages.Count);
            Assert.AreEqual(25, result);
            Assert.AreEqual("s5", val);
            Assert.IsTrue(infoMassages[1].Contains("25"));
            Assert.IsTrue(infoMassages[1].Contains("stringParam:s5"));
        }

        [Test]
        public void LogInfo_WhenRefParameter()
        {
            var testClass = Substitute.For<ITestClass>();
            var val = "s2";
            testClass.MethodWithRefParameter(ref val)
                .Returns(x =>
                {
                    x[0] = "s5";

                    return 25;
                });

            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString());

            var result = decorated.MethodWithRefParameter(ref val);

            var val2 = "s2";
            testClass.Received().MethodWithRefParameter(ref val2);
            Assert.AreEqual(0, errorMassages.Count);
            Assert.AreEqual(2, infoMassages.Count);
            Assert.AreEqual(25, result);
            Assert.AreEqual("s5", val);
            Assert.IsTrue(infoMassages[0].Contains("stringParam:s2"));
            Assert.IsTrue(infoMassages[1].Contains("25"));
            Assert.IsTrue(infoMassages[1].Contains("stringParam:s5"));
        }

        [Test]
        public void LogInfo_WhenOutParameterOfValueType()
        {
            var testClass = Substitute.For<ITestClass>();
            var val = 2;
            testClass.MethodWithOutParameter(out val)
                .Returns(x =>
                {
                    x[0] = 5;

                    return 25;
                });

            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString());

            var result = decorated.MethodWithOutParameter(out val);

            var val2 = 2;
            testClass.Received().MethodWithOutParameter(out val2);
            Assert.AreEqual(0, errorMassages.Count);
            Assert.AreEqual(2, infoMassages.Count);
            Assert.AreEqual(25, result);
            Assert.AreEqual(5, val);
            Assert.IsTrue(infoMassages[1].Contains("25"));
            Assert.IsTrue(infoMassages[1].Contains("intParam:5"));
        }

        [Test]
        public void LogInfo_WhenRefParameterOfValueType()
        {
            var testClass = Substitute.For<ITestClass>();
            var val = 2;
            testClass.MethodWithRefParameter(ref val)
                .Returns(x =>
                {
                    x[0] = 5;

                    return 25;
                });

            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString());

            var result = decorated.MethodWithRefParameter(ref val);

            var val2 = 2;
            testClass.Received().MethodWithRefParameter(ref val2);
            Assert.AreEqual(0, errorMassages.Count);
            Assert.AreEqual(2, infoMassages.Count);
            Assert.AreEqual(25, result);
            Assert.AreEqual(5, val);
            Assert.IsTrue(infoMassages[0].Contains("intParam:2"));
            Assert.IsTrue(infoMassages[1].Contains("25"));
            Assert.IsTrue(infoMassages[1].Contains("intParam:5"));
        }

        [Test]
        public void LogInfo_WhenMixedParameters()
        {
            var testClass = Substitute.For<ITestClass>();
            var refParam = 0;
            var outParam = "s2";
            testClass.MethodWithMixedParameter(ref refParam, out outParam, true)
                .Returns(x =>
                {
                    x[0] = 5;
                    x[1] = "s5";

                    return 25;
                });

            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString());

            var result = decorated.MethodWithMixedParameter(ref refParam, out outParam, true);

            var refParam2 = 0;
            var outParam2 = "s2";
            testClass.Received().MethodWithMixedParameter(ref refParam2, out outParam2, true);
            Assert.AreEqual(0, errorMassages.Count);
            Assert.AreEqual(2, infoMassages.Count);
            Assert.AreEqual(25, result);
            Assert.AreEqual(5, refParam);
            Assert.AreEqual("s5", outParam);
            Assert.IsTrue(infoMassages[0].Contains("refParam:0"));
            Assert.IsTrue(infoMassages[0].Contains("outParam:s2"));
            Assert.IsTrue(infoMassages[0].Contains("param:True"));
            Assert.IsTrue(infoMassages[1].Contains("25"));
            Assert.IsTrue(infoMassages[1].Contains("refParam:5"));
            Assert.IsTrue(infoMassages[1].Contains("outParam:s5"));
            Assert.IsTrue(infoMassages[1].Contains("param:True"));
        }

        [Test]
        public void LogInfoWithProperClassName()
        {
            var testClass = new TestClass2();
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass2>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString());

            decorated.Method();

            Assert.AreEqual(2, infoMassages.Count);
            Assert.IsTrue(infoMassages[0].Contains("Class AOP.Tests.UsingRealProxy.TestClass2"));
            Assert.IsTrue(infoMassages[1].Contains("Class AOP.Tests.UsingRealProxy.TestClass2"));
            Assert.AreEqual(0, errorMassages.Count);
        }

        [Test]
        public void LogInfoWithProperStackTrace()
        {
            var testClass = new TestClass2();
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass2>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString());

            Assert.Throws<Exception>(() => decorated.MethodWithException());

            Assert.AreEqual(1, infoMassages.Count);
            Assert.IsTrue(infoMassages[0].Contains("Class AOP.Tests.UsingRealProxy.TestClass2"));
            Assert.AreEqual(1, errorMassages.Count);
            Assert.IsTrue(errorMassages[0].Contains("Class AOP.Tests.UsingRealProxy.TestClass2"));
            //Only original Exception should be logged
            Assert.IsFalse(errorMassages[0].Contains("LoggingAdvice.cs"));
            Assert.IsTrue(errorMassages[0].Contains("LoggingAdviceTests.cs"));
        }

        [Test]
        public void LogInfoWithProperStackTrace_WhenTask()
        {
            var testClass = new TestClass2();
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass2>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString(),
                _taskScheduler);

            Assert.Throws<AggregateException>(() => decorated.AsyncMethodWithException().Wait());

            Assert.AreEqual(1, infoMassages.Count);
            Assert.IsTrue(infoMassages[0].Contains("Class AOP.Tests.UsingRealProxy.TestClass2"));
            Assert.AreEqual(1, errorMassages.Count);
            Assert.IsTrue(errorMassages[0].Contains("Class AOP.Tests.UsingRealProxy.TestClass2"));
            //Only original Exception should be logged
            Assert.IsFalse(errorMassages[0].Contains("LoggingAdvice.cs"));
            Assert.IsTrue(errorMassages[0].Contains("LoggingAdviceTests.cs"));
        }

        [Test]
        public void LogInfo_WhenPropertySet()
        {
            var testClass = Substitute.For<ITestClass>();
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString());

            decorated.Property = "Str1";

            testClass.Received().Property = "Str1";
            Assert.AreEqual(2, infoMassages.Count);
            Assert.IsTrue(infoMassages[0].Contains("set_Property"));
            Assert.IsTrue(infoMassages[1].Contains("set_Property"));
            Assert.AreEqual(0, errorMassages.Count);
        }

        [Test]
        public void LogInfo_WhenPropertyGet()
        {
            var testClass = Substitute.For<ITestClass>();
            testClass.Property.Returns("Str1");
            var errorMassages = new List<string>();
            var infoMassages = new List<string>();
            var decorated = LoggingAdvice<ITestClass>.Create(
                testClass,
                s => infoMassages.Add(s),
                s => errorMassages.Add(s),
                o => o?.ToString());

            var value = decorated.Property;

            var tmp = testClass.Received().Property;
            Assert.AreEqual("Str1", value);
            Assert.AreEqual(2, infoMassages.Count);
            Assert.IsTrue(infoMassages[0].Contains("get_Property"));
            Assert.IsTrue(infoMassages[1].Contains("get_Property"));
            Assert.AreEqual(0, errorMassages.Count);
        }

        /// <summary>
        /// Call to process scheduled tasks
        /// </summary>
        protected void ReleaseContext()
        {
            Task.Delay(1).Wait();
        }
    }
}
#endif