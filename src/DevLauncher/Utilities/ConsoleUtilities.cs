using System;

namespace RepublicAtWar.DevLauncher.Utilities;

internal static class ConsoleUtilities
{
    public delegate bool ConsoleQuestionValueFactory<T>(string input, out T value);

    public static void WriteHorizontalLine(char lineChar = '─', int length = 20)
    {
        var line = new string(lineChar, length);
        Console.WriteLine(line);
    }
    
    public static T UserQuestionOnSameLine<T>(string question, ConsoleQuestionValueFactory<T> inputCorrect)
    {
        while (true)
        {
            var promptLeft = 0;
            var promptTop = Console.CursorTop;

            Console.SetCursorPosition(promptLeft, promptTop);
            Console.Write(question);
            Console.SetCursorPosition(promptLeft + question.Length, promptTop);

            var input = ReadLineInline();

            if (!inputCorrect(input, out var result))
            {
                Console.SetCursorPosition(0, promptTop);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                continue;
            }

            Console.WriteLine();
            return result;
        }
    }

    private static string ReadLineInline()
    {
        var input = "";
        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
                break;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                {
                    input = input[..^1];
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    Console.Write(' ');
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                input += key.KeyChar;
                Console.Write(key.KeyChar);
            }
        }

        return input;
    }
}