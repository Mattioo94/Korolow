using static System.Console;

namespace Konsola
{
    class Program
    {
        private static Korolow.Script script = new Korolow.Script();

        static void Main(string[] args)
        {
            script.Error += OnError;
            script.Success += OnSuccess;

            script.Add("script");
            script.Exec();
        }

        private static void OnError(object o, Korolow.ErrorEventArgs e) =>
            WriteLine($"Error: {e.Error.Message}");

        private static void OnSuccess(object o, Korolow.SuccessEventArgs e) =>
            WriteLine($"Output: {e.Result}Time: {e.Time}");
    }
}