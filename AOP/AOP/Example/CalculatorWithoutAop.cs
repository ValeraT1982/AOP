namespace AOP.Example
{
    public class CalculatorWithoutAop: ICalculator
    {
        private readonly ILogger _logger;

        public CalculatorWithoutAop(ILogger logger)
        {
            _logger = logger;
        }

        public int Add(int a, int b)
        {
            _logger.Log($"Adding {a} + {b}");
            var result = a + b;
            _logger.Log($"Result is {result}");

            return result;
        }

        public int Subtract(int a, int b)
        {
            _logger.Log($"Subtracting {a} - {b}");
            var result = a - b;
            _logger.Log($"Result is {result}");

            return result;
        }
    }
}
