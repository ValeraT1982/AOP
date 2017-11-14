namespace AOP.Example
{
    public class MyClass : IMyClass
    {
        public int MyMethod(string param)
        {
            return param.Length;
        }

        //public static void Example()
        //{
        //    var decorated = LoggingAdvice<IMyClass>.Create(
        //        new MyClass(),
        //        s => Debug.WriteLine("Info:" + s),
        //        s => Debug.WriteLine("Error:" + s),
        //        o => o?.ToString());

        //    var length = decorated.MyMethod("Hello world!");
        //}
    }
}
