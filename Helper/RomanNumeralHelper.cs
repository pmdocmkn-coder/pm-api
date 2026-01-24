namespace Pm.Helper
{
    public static class RomanNumeralHelper
    {
        private static readonly string[] RomanMonths = new[]
        {
            "I", "II", "III", "IV", "V", "VI",
            "VII", "VIII", "IX", "X", "XI", "XII"
        };

        public static string ToRoman(int month)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month),
                    "Month must be between 1 and 12.");
            }

            return RomanMonths[month - 1];
        }
    }
}
