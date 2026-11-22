using System.Text;

namespace Evo.Engine.Host;

public static class Program
{
    public static int Main()
    {
        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        Console.InputEncoding = utf8NoBom;
        Console.OutputEncoding = utf8NoBom;

        var processor = new RequestProcessor();
        string? line;
        while ((line = Console.In.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            string? response = processor.Process(line);
            if (response is not null)
            {
                Console.Out.WriteLine(response);
                Console.Out.Flush();
            }
        }
        return 0;
    }
}
