using System;
using System.Text.RegularExpressions;

namespace yourvrexperience.Utils
{    

    public class NumberToWordsConverter
    {
        private static readonly string[] Units =
        {
            "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
        };

        private static readonly string[] Tens =
        {
            "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
        };

        private static readonly string[] LargeNumbers =
        {
            "", "Thousand", "Million", "Billion", "Trillion"
        };

        public static string ReplaceNumbersWithWords(string input)
        {
            return Regex.Replace(input, @"\d+(\.\d+)?", match => ConvertNumberToWords(match.Value));
        }

        private static string ConvertNumberToWords(string numberStr)
        {
            if (numberStr.Contains("."))
            {
                string[] parts = numberStr.Split('.');
                long wholePart = long.Parse(parts[0]);
                string decimalPart = parts[1];

                string wholePartWords = NumberToWords(wholePart);
                string decimalPartWords = ConvertDecimalPart(decimalPart);

                return wholePartWords + "-Point-" + decimalPartWords;
            }
            else
            {
                return NumberToWords(long.Parse(numberStr));
            }
        }

        private static string ConvertDecimalPart(string decimalStr)
        {
            char[] digits = decimalStr.ToCharArray();
            string result = "";
            foreach (char digit in digits)
            {
                result += Units[int.Parse(digit.ToString())] + "-";
            }
            return result.TrimEnd('-');
        }

        private static string NumberToWords(long number)
        {
            if (number == 0)
                return "Zero";

            return ConvertToWords(number).Trim();
        }

        private static string ConvertToWords(long number)
        {
            if (number < 20)
                return Units[number];

            if (number < 100)
                return Tens[number / 10] + (number % 10 > 0 ? "-" + Units[number % 10] : "");

            if (number < 1000)
                return Units[number / 100] + "-Hundred" + (number % 100 > 0 ? "-And-" + ConvertToWords(number % 100) : "");

            for (int i = 1; i < LargeNumbers.Length; i++)
            {
                long unitValue = (long)Math.Pow(1000, i);
                if (number < unitValue * 1000)
                    return ConvertToWords(number / unitValue) + "-" + LargeNumbers[i] + (number % unitValue > 0 ? "-And-" + ConvertToWords(number % unitValue) : "");
            }

            return "Number out of supported range.";
        }

        public static void Main()
        {
            string text = "The price of the item is 97 dollars. The year 1048 was historic. Population: 64126. The height is 3.14 meters.";
            string result = ReplaceNumbersWithWords(text);

            Console.WriteLine(result);
        }
    }
}