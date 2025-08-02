public class Test2
{
    private readonly ICalculator _calculator;

    public Test2(ICalculator calculator)
    {
        _calculator = calculator;
    }

    public int Add(int a, int b)
    {
        return _calculator.Calculate(a, b);
    }
}

public interface ICalculator
{
    int Calculate(int a, int b);
}

public class SimpleAdder : ICalculator
{
    public int Calculate(int a, int b)
    {
        return a + b;
    }
}
