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
            var dp = new DataProcessor();
            dp.doall(@"c:/br/CNY6.24/", @"c:/br/CNY6.24/_Price.txt", 1);
            var zz = dp.SerializeTickRecords();

            int n = dp.foundNum("20240205", "173900");

            var ttt = JsonConvert.SerializeObject(dp.json(n, "18.07.24"), Formatting.Indented) + ",{ updatePeriod: 1200000}";

            var errors = string.Join("", dp.errors);

            File.WriteAllText("c:/br/cny_out.csv", zz + "\r\n\r\n\r\n\r\n\r\n" + errors);
            File.WriteAllText("c:/br/json", ttt.Replace('"', '\''));

            /*
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

            Console.WriteLine("Processing complete.");*/

        }
    }
}
