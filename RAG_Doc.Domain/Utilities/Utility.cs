using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RAG_Doc.Domain.Utilities
{
    public static class Utility
    {
        public static byte[] ConvertFloatArrayToBytes(float[] array)
        {
            byte[] bytes = new byte[array.Length * sizeof(float)];
            Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static float[] ConvertBytesToFloatArray(byte[] bytes)
        {
            float[] array = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
            return array;
        }

        public static double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same length");

            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                magnitudeA += a[i] * a[i];
                magnitudeB += b[i] * b[i];
            }

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0;

            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }

        public static string CreateContentHash(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
        }

        public static float[] Normalize(float[] vector)
        {
            float norm = (float)Math.Sqrt(vector.Sum(x => x * x));
            if (norm == 0) return vector; // Avoid divide-by-zero
            for (int i = 0; i < vector.Length; i++)
                vector[i] /= norm;
            return vector;
        }

        // Truncate text to approximately `maxTokens` words
        public static string TruncateText(string text, int maxWords = 200)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= maxWords) return text;

            return string.Join(' ', words.Take(maxWords)) + "...";
        }

        public static string RemoveThinkBlocks(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return Regex.Replace(
                text,
                "<think>.*?</think>",
                string.Empty,
                RegexOptions.Singleline | RegexOptions.IgnoreCase)
                .Trim();
        }

        public static string BuildDeterministicKey(string prefix, string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return $"{prefix}:{Convert.ToHexString(bytes)}";
        }
    }
}
