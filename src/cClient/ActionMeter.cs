using System.Diagnostics;

namespace cClient
{
    internal class ActionMeter : IDisposable
    {
        readonly string name;
        readonly Stopwatch stopwatch;

        public TimeSpan Elapsed => this.stopwatch.Elapsed;

        public ActionMeter(string name)
        {
            this.name = name;
            this.stopwatch = Stopwatch.StartNew();
        }
        public void Dispose()
        {
            Clear();
            this.stopwatch.Stop();
        }

        public void Print(string text)
        {
            var width = Console.WindowWidth;
            var name_format = $"{this.name} ";
            var time_format = $" ({(int)this.stopwatch.Elapsed.TotalSeconds} s)";
            var text_width = width - name_format.Length - time_format.Length;
            var format = $"{name_format}{CutFormat(text, text_width, out var pad).PadLeft(pad)}{time_format}";

            Console.Write($"\r{format}");
        }
        public void Clear()
        {
            var width = Console.WindowWidth;
            Console.Write("\r");
            Console.Write(new string(' ', width));
            Console.Write("\r");
        }

        static string CutFormat(string text, int width, out int pad)
        {
            pad = width;
            var actual = 0;
            var index = 0;
            for (var i = text.Length - 1; i >= 0; i--)
            {
                if (IsWideChar(text.ElementAt(i)))
                {
                    actual = actual + 2;
                    pad = pad - 1;
                }
                else
                {
                    actual = actual + 1;
                }
                if (actual <= width)
                {
                    index = i;
                }
                else
                {
                    break;
                }
            }

            return text.Substring(index);
        }
        static bool IsWideChar(char ch)
        {
            // CJK Unified Ideographs
            if (ch >= '\u4E00' && ch <= '\u9FFF') return true;

            // CJK Symbols and Punctuation
            if (ch >= '\u3000' && ch <= '\u303F') return true;

            // Hiragana / Katakana
            if (ch >= '\u3040' && ch <= '\u30FF') return true;

            // Fullwidth Forms
            if (ch >= '\uFF01' && ch <= '\uFF60') return true;
            if (ch >= '\uFFE0' && ch <= '\uFFE6') return true;

            // Hangul Syllables
            if (ch >= '\uAC00' && ch <= '\uD7AF') return true;

            return false;
        }
    }
}
