using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public static PortfolioData SerializeTickRecords(TickRecord tickRecords, List<Additional> add, int line, string exp)
        {
            PortfolioData pp
                = new PortfolioData()
                {
                    Name = $"{tickRecords.Ticker},{tickRecords.DateTime.ToShortDateString()},{tickRecords.DateTime.ToShortTimeString()}",
                    Filter = "custom",
                    Description = $"Создан {DateTime.Now.Date.ToShortDateString()}",
                    AssetPrice = tickRecords.Close,


                };


            pp.Portfolio = add.Where(x => x.list[line].Oi > 0).Select(x =>
                  new PortfolioItem()
                  {
                      Type = "option",
                      Code = "&amp;nbsp;",
                      AssetPrice = tickRecords.Close,
                      Expiration = exp,
                      Price = x.list[line].MP,
                      OptionType = x.opt.ToLower(),
                      Strike = x.num,
                      Quantity = (int)x.list[line].Oi
                  }).ToList();

            return pp;

        }


        static void Main(string[] args)
        {
            Dictionary<string, string> parameters = ParseArgs(args);

            if (!parameters.ContainsKey("inputdirectory") ||
                !parameters.ContainsKey("inputfile") ||
                !parameters.ContainsKey("outputfile"))
            {
                Console.WriteLine("Usage: Options -inputDirectory <inputDirectory> -inputFile <inputFile> -period <period> -outputFile <outputFile>  -analyzer <outputFile> -seek <20240205_173900> -exp <18.07.24> ");
                return;
            }

            string inputDirectory = AddTrailingSlash(parameters["inputdirectory"]);
            string inputFile = parameters["inputfile"];

            string outputFile = parameters["outputfile"];

            var processor = new DataProcessor();
            processor.doall(inputDirectory, inputFile, 1);


            var zz = processor.SerializeTickRecords();
            var errors = string.Join("", processor.errors);

            File.WriteAllText(outputFile, zz + "\r\n\r\n\r\n\r\n\r\n" + errors);






            if (parameters.ContainsKey("analyzer"))
            {
                if (!parameters.ContainsKey("seek") ||
                !parameters.ContainsKey("exp"))
                {
                    Console.WriteLine("Usage: Options -inputDirectory <inputDirectory> -inputFile <inputFile> -period <period> -outputFile <outputFile>  -analyzer <outputFile> -seek <20240205_173900> -exp <18.07.24> ");
                    return;
                }

                var dates = parameters["seek"].Split('_');

                int n = processor.foundNum(dates[0], dates[1]);

                var ttt = JsonConvert.SerializeObject(processor.json(n, parameters["exp"]), Formatting.Indented) + ",{ updatePeriod: 1200000}";
                File.WriteAllText(parameters["analyzer"], ttt.Replace('"', '\''));


                return;
            }














            Console.WriteLine("Processing complete.");
        }

        static Dictionary<string, string> ParseArgs(string[] args)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i += 2)
            {
                if (i + 1 < args.Length)
                {
                    parameters[args[i].TrimStart('-').ToLower()] = args[i + 1];
                }
            }
            return parameters;
        }




    }
}
