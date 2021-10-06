using System;
using System.Collections.Generic;
using System.Linq;

namespace Speech_Recognition.Data.Functions
{
    public class StrNumberToInt
    {
        private String strNumber; // String number to convert
        // Define arrays of keywords to translate text words to integer positions in the arrays
        private String[] ones = { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
        private String[] teens = { "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        private String[] tens = { "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
        private Dictionary<String, int> bigScales = new Dictionary<String, int>() { { "hundred", 100 }, { "hundreds", 100 }, 
            { "thousand", 1000 }, { "million", 1000000 }, { "billion", 1000000000 } };
        private String[] minusWords = { "minus", "negative" };
        private char[] splitchars = new char[] { ' ', '-', ',' };

        public StrNumberToInt(String _number) { strNumber = _number; }

        public int getIntFromString() {
            // Flip all words to lowercase for proper matching
            var lowercase = strNumber.ToLower();
            var inputwords = lowercase.Split(splitchars, StringSplitOptions.RemoveEmptyEntries);
            // Initalize loop variables and flags
            int result = 0;
            int currentResult = 0;
            int bigMultiplierValue = 1;
            bool bigMultiplierIsActive = false;
            bool minusFlag = false;
            foreach (String curword in inputwords) {
                // input words are either bigMultipler words or little words
                if (bigScales.ContainsKey(curword)) {
                    bigMultiplierValue *= bigScales[curword];
                    bigMultiplierIsActive = true;
                } else {
                    // multiply the current result by the previous word bigMultiplier
                    // and disable the big multiplier until next time
                    if (bigMultiplierIsActive) {
                        result += currentResult * bigMultiplierValue;
                        currentResult = 0;
                        bigMultiplierValue = 1; // reset the multiplier value
                        bigMultiplierIsActive = false; // turn it off until next time
                    }
                    // translate the incoming text word to an integer
                    int n;
                    if ((n = Array.IndexOf(ones, curword) + 1) > 0) currentResult += n;
                    else if ((n = Array.IndexOf(teens, curword) + 1) > 0) currentResult += n + 10;
                    else if ((n = Array.IndexOf(tens, curword) + 1) > 0) currentResult += n * 10;
                    // allow for negative words (like "minus") 
                    else if (minusWords.Contains(curword)) minusFlag = true;
                    // allow for phrases like "zero 500" hours military time
                    else if (curword == "zero") continue;
                    // allow for text digits too, like "100 and 5"
                    else if (int.TryParse(curword, out int tmp)) currentResult += tmp;
                    else if (curword != "and")
                        throw new ApplicationException("Expected a number: " + curword);
                }
            }
            var intNumber = result + currentResult * bigMultiplierValue;
            return minusFlag ? (intNumber *= -1) : intNumber;
        }
    }
}