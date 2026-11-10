using System.Globalization;
using System.Text;

namespace GamesLab4.Core.Numbers;

/// <summary>Неотрицательное целое: младший блок первым, основание 1000.
/// Арифметика выполняется вручную с int, без long, BigInteger и double.</summary>
public sealed class BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
{
    private const int Base = 1000;
    private readonly int[] number;
    public static BigNumber Zero { get; } = new(0);
    public static BigNumber One { get; } = new(1);

    public BigNumber(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        var blocks = new List<int>();
        do { blocks.Add(value % Base); value /= Base; } while (value > 0);
        number = blocks.ToArray();
    }

    public BigNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        value = value.Trim();
        if (value.Length == 0 || value.Any(c => c is < '0' or > '9'))
            throw new FormatException("Ожидалось неотрицательное целое число.");
        value = value.TrimStart('0');
        if (value.Length == 0) value = "0";
        number = new int[(value.Length + 2) / 3];
        for (int i = 0, end = value.Length; end > 0; i++, end -= 3)
        {
            int start = Math.Max(0, end - 3);
            number[i] = int.Parse(value.AsSpan(start, end - start), CultureInfo.InvariantCulture);
        }
    }

    private BigNumber(int[] blocks)
    {
        int length = blocks.Length;
        while (length > 1 && blocks[length - 1] == 0) length--;
        number = blocks[..length]; // Копия массива защищает неизменяемость.
    }

    public BigNumber Clone() => new(number);
    public override string ToString()
    {
        var result = new StringBuilder(number[^1].ToString(CultureInfo.InvariantCulture));
        for (int i = number.Length - 2; i >= 0; i--)
            result.Append(number[i].ToString("D3", CultureInfo.InvariantCulture));
        return result.ToString();
    }

    public int CompareTo(BigNumber? other)
    {
        if (other is null) return 1;
        if (number.Length != other.number.Length) return number.Length.CompareTo(other.number.Length);
        for (int i = number.Length - 1; i >= 0; i--)
            if (number[i] != other.number[i]) return number[i].CompareTo(other.number[i]);
        return 0;
    }

    public bool Equals(BigNumber? other) => other is not null && CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is BigNumber other && Equals(other);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (int block in number) hash.Add(block);
        return hash.ToHashCode();
    }
    public static bool operator ==(BigNumber? a, BigNumber? b) => ReferenceEquals(a, b) || (a?.Equals(b) ?? false);
    public static bool operator !=(BigNumber? a, BigNumber? b) => !(a == b);
    public static bool operator <(BigNumber a, BigNumber b) => a.CompareTo(b) < 0;
    public static bool operator >(BigNumber a, BigNumber b) => a.CompareTo(b) > 0;
    public static bool operator <=(BigNumber a, BigNumber b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BigNumber a, BigNumber b) => a.CompareTo(b) >= 0;
    public static BigNumber operator +(BigNumber a, BigNumber b) => a.Add(b);
    public static BigNumber operator -(BigNumber a, BigNumber b) => a.Subtract(b);
    public static BigNumber operator *(BigNumber a, double b) => a.Multiply(b);
    public static BigNumber operator /(BigNumber a, double b) => a.Divide(b);

    private BigNumber Add(BigNumber other)
    {
        ArgumentNullException.ThrowIfNull(other);
        int[] result = new int[Math.Max(number.Length, other.number.Length) + 1];
        int carry = 0;
        for (int i = 0; i < result.Length; i++)
        {
            int sum = carry + (i < number.Length ? number[i] : 0)
                + (i < other.number.Length ? other.number[i] : 0);
            result[i] = sum % Base;
            carry = sum / Base;
        }
        return new(result);
    }

    private BigNumber Subtract(BigNumber other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (this < other) throw new ArgumentOutOfRangeException(nameof(other), "Результат не может быть отрицательным.");
        int[] result = new int[number.Length];
        int borrow = 0;
        for (int i = 0; i < number.Length; i++)
        {
            int difference = number[i] - borrow - (i < other.number.Length ? other.number[i] : 0);
            borrow = difference < 0 ? 1 : 0;
            result[i] = difference + borrow * Base;
        }
        return new(result);
    }

    private BigNumber Multiply(double multiplier)
    {
        var (numerator, denominator) = ToFraction(multiplier);
        return MultiplyBlocks(numerator).DivideBlocks(denominator);
    }

    private BigNumber Divide(double divisor)
    {
        var (numerator, denominator) = ToFraction(divisor);
        if (numerator == Zero) throw new DivideByZeroException();
        return MultiplyBlocks(denominator).DivideBlocks(numerator);
    }

    private BigNumber MultiplyBlocks(BigNumber other)
    {
        int[] result = new int[number.Length + other.number.Length + 1];
        for (int i = 0; i < number.Length; i++)
        {
            int carry = 0;
            for (int j = 0; j < other.number.Length; j++)
            {
                int sum = result[i + j] + number[i] * other.number[j] + carry;
                result[i + j] = sum % Base;
                carry = sum / Base;
            }
            int index = i + other.number.Length;
            while (carry > 0)
            {
                int sum = result[index] + carry;
                result[index++] = sum % Base;
                carry = sum / Base;
            }
        }
        return new(result);
    }

    private BigNumber MultiplyBlock(int block)
    {
        int[] result = new int[number.Length + 1];
        int carry = 0;
        for (int i = 0; i < number.Length; i++)
        {
            int product = number[i] * block + carry;
            result[i] = product % Base;
            carry = product / Base;
        }
        result[^1] = carry;
        return new(result);
    }

    private BigNumber DivideBlocks(BigNumber divisor)
    {
        if (divisor == Zero) throw new DivideByZeroException();
        int[] quotient = new int[number.Length];
        BigNumber remainder = Zero;
        for (int i = number.Length - 1; i >= 0; i--)
        {
            // Остаток * 1000 + очередной блок.
            int[] shifted = new int[remainder.number.Length + 1];
            Array.Copy(remainder.number, 0, shifted, 1, remainder.number.Length);
            shifted[0] = number[i];
            remainder = new(shifted);
            int low = 0, high = Base - 1;
            while (low <= high)
            {
                int middle = (low + high) / 2;
                if (divisor.MultiplyBlock(middle) <= remainder) low = middle + 1;
                else high = middle - 1;
            }
            quotient[i] = high;
            remainder = remainder - divisor.MultiplyBlock(high);
        }
        return new(quotient);
    }

    private static (BigNumber Numerator, BigNumber Denominator) ToFraction(double value)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        // double используется только на границе API. Переводим коэффициент
        // из десятичной записи в целую дробь и дальше считаем блоками int.
        string text = value.ToString("R", CultureInfo.InvariantCulture);
        int e = text.IndexOfAny(['E', 'e']);
        int exponent = e >= 0 ? int.Parse(text[(e + 1)..], CultureInfo.InvariantCulture) : 0;
        if (e >= 0) text = text[..e];
        int dot = text.IndexOf('.');
        int scale = (dot >= 0 ? text.Length - dot - 1 : 0) - exponent;
        string digits = text.Replace(".", "");
        return scale >= 0
            ? (new BigNumber(digits), new BigNumber("1" + new string('0', scale)))
            : (new BigNumber(digits + new string('0', -scale)), One);
    }
}
