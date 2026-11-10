using GamesLab4.Core.Numbers;

namespace GamesLab4.Checks;

internal static class NumberChecks
{
    [Check("BigNumber: примеры арифметики из Лабы 2")]
    public static void Examples()
    {
        Verify.Equal("19225017", (new BigNumber("3712117") + new BigNumber("15512900")).ToString());
        Verify.Equal("1800217", (new BigNumber("3712117") - new BigNumber("1911900")).ToString());
        Verify.Equal("3338072", (new BigNumber("10014217") / 3).ToString());
        Verify.Equal("164120042", (new BigNumber("7135654") * 23).ToString());
        Verify.Equal("1999999999", (new BigNumber("1000000000") + new BigNumber("999999999")).ToString());
    }
    [Check("BigNumber: нули, формат D3 и границы блоков")]
    public static void Formatting()
    {
        Verify.Equal("19005017", new BigNumber("00019005017").ToString());
        Verify.Equal("0", new BigNumber("000").ToString());
        Verify.Equal("2147483647", new BigNumber(int.MaxValue).ToString());
        Verify.Equal("0", (new BigNumber(1000) - new BigNumber(1000)).ToString());
        Verify.Equal("1000000", (new BigNumber(999999) + BigNumber.One).ToString());
    }
    [Check("BigNumber: неизменяемость и огромные числа")]
    public static void Immutable()
    {
        var original = new BigNumber("1" + new string('0', 90));
        Verify.Equal(new string('9', 90), (original - BigNumber.One).ToString());
        Verify.Equal("1" + new string('0', 90), original.ToString());
        Verify.True(original == original.Clone());
        Verify.True(!ReferenceEquals(original, original.Clone()));
        Verify.Equal("1" + new string('0', 93), (original * 1000).ToString());
    }
    [Check("BigNumber: дробные коэффициенты и научная запись")]
    public static void Fractions()
    {
        Verify.Equal("88", (new BigNumber(80) * 1.1).ToString());
        Verify.Equal("96", ((new BigNumber(80) * 1.1) * 1.1).ToString());
        Verify.Equal("17", (new BigNumber(7) / 0.4).ToString());
        Verify.Equal("7", (new BigNumber(7) * 1.05).ToString());
        Verify.Equal("1000000", (new BigNumber(10) * 1e5).ToString());
        Verify.Equal("1", (new BigNumber(1000000) * 1e-6).ToString());
        Verify.Equal("0", (BigNumber.One * double.Epsilon).ToString());
    }
    [Check("BigNumber: сверка 500 примеров с арифметикой int")]
    public static void CrossCheck()
    {
        var random = new Random(21);
        for (int i = 0; i < 500; i++)
        {
            int a = random.Next(1, 1000000), b = random.Next(1, 1000);
            var x = new BigNumber(a); var y = new BigNumber(b);
            Verify.Equal((a + b).ToString(), (x + y).ToString());
            Verify.Equal((a - b >= 0 ? a - b : b - a).ToString(), (a >= b ? x - y : y - x).ToString());
            Verify.Equal((a * b).ToString(), (x * b).ToString());
            Verify.Equal((a / b).ToString(), (x / b).ToString());
        }
    }
    [Check("BigNumber: недопустимые значения отклоняются")]
    public static void Invalid()
    {
        Verify.Throws<DivideByZeroException>(() => { _ = BigNumber.One / 0; });
        Verify.Throws<ArgumentOutOfRangeException>(() => { _ = BigNumber.One - new BigNumber(2); });
        Verify.Throws<ArgumentOutOfRangeException>(() => { _ = BigNumber.One * double.NaN; });
        Verify.Throws<ArgumentOutOfRangeException>(() => { _ = BigNumber.One * -1; });
        Verify.Throws<FormatException>(() => new BigNumber("1.5"));
    }
}
