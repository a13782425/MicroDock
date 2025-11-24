using System.Text;

namespace FileToolPlugin.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: FileToolPlugin.Console <command> [args]");
                return;
            }

            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "read":
                        if (args.Length < 2)
                        {
                            Console.Error.WriteLine("Usage: read <filepath>");
                            return;
                        }
                        string readPath = args[1];
                        if (File.Exists(readPath))
                        {
                            byte[] bytes = File.ReadAllBytes(readPath);
                            string base64 = Convert.ToBase64String(bytes);
                            Console.Write(base64); // Write to stdout without newline if possible, or just WriteLine
                        }
                        else
                        {
                            Console.Error.WriteLine($"File not found: {readPath}");
                        }
                        break;

                    case "write":
                        if (args.Length < 3)
                        {
                            Console.Error.WriteLine("Usage: write <filepath> <base64_content>");
                            return;
                        }
                        string writePath = args[1];
                        string content = args[2];
                        byte[] writeBytes = Convert.FromBase64String(content);
                        File.WriteAllBytes(writePath, writeBytes);
                        Console.WriteLine("Success");
                        break;

                    default:
                        Console.Error.WriteLine($"Unknown command: {command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
