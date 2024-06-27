using System;
using System.IO;

namespace Options
{
    internal class Program
    {
        public static string AddTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            }

            if (!path.EndsWith("\\") && !path.EndsWith("/"))
            {
                path += "\\";
            }

            return path;
        }
        static void Main(string[] args)
        {
            /*
            var zz = DataProcessor.doall(@"d:/br/", @"d:/br/_price.txt", 15);

            File.WriteAllText("d:/br/999.csv", zz);*/

            if (args.Length != 4)
            {
                Console.WriteLine("Usage: Options <inputDirectory> <inputFile> <period> <outputFile>");
                return;
            }

            string inputDirectory = AddTrailingSlash(args[0]);
            string inputFile = args[1];
            if (!int.TryParse(args[2], out int period))
            {
                Console.WriteLine("Error: <period> must be an integer.");
                return;
            }
            string outputFile = args[3];

            var zz = DataProcessor.doall(inputDirectory, inputFile, period);

            File.WriteAllText(outputFile, zz);

            Console.WriteLine("Processing complete.");

        }
    }
}
