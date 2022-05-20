// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System.Linq;

    public static class NamingRestrictions
    {
        public const string InvalidNamingStringErrorMessage = "A valid string conforms with the following regex: ^[a-zA-Z0-9_-]+$";

        public static readonly char[] NoAdditional = new char[0];
        public static readonly char[] CharacterSetA = new[] { '.' };
        public static readonly char[] CharacterSetB = new[] { ':' };

        /// <summary>
        /// Verifies that the input string makes use of valid naming characters.
        /// A valid string conforms with the following regex: ^[a-zA-Z0-9_-]+$
        /// </summary>
        public static bool IsValidNamingString(string input, char[] additionalChars)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            // roughly 2-3x faster than doing it with Regex from some perf testing with 100k strings of "usual" length.
            foreach (char curC in input)
            {
                bool isValid = (curC >= '0' && curC <= '9') ||
                    (curC >= 'A' && curC <= 'Z') ||
                    (curC >= 'a' && curC <= 'z') ||
                    curC == '-' ||
                    curC == '_' ||
                    additionalChars.Contains(curC);

                if (!isValid)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsValidNamingString(string input)
        {
            return IsValidNamingString(input, NoAdditional);
        }

        public static bool IsValidDeviceID(string input)
        {
            return IsValidNamingString(input, CharacterSetB);
        }

        public static bool IsAllowedCertificateName(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length > 64)
            {
                return false;
            }

            return IsValidNamingString(value, CharacterSetA);
        }
    }
}