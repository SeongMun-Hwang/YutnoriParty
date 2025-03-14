using UnityEngine;

public interface IOperatorStrategy
{
    public bool Calc(int a, int b, int result);
}

public class AddOperatorStrategy : IOperatorStrategy
{
    public bool Calc(int a, int b, int result)
    {
        return (a + b == result);
    }
}

public class SubtractOperatorStrategy : IOperatorStrategy
{
    public bool Calc(int a, int b, int result)
    {
        return (a - b == result);
    }
}

public class MultiplyOperatorStrategy : IOperatorStrategy
{
    public bool Calc(int a, int b, int result)
    {
        return (a * b == result);
    }
}
