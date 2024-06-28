using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Options.DataProcessor;

namespace Options
{
    public class Additional
    {
        public decimal num;
        public string opt;
        public List<OutputTickRecord> list;
    }
    public class TickRecord
    {
        public string Ticker { get; set; }
        public int Per { get; set; }
        public DateTime DateTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Vol { get; set; }
    }

    public class SimpleTickRecord
    {
        public string Ticker { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public decimal CloseOI { get; set; }
        public decimal CloseDifference { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal Volume { get; set; }
    }

    public static class DataProcessor
    {
        public static void FillCloseWithPrevious(List<TickRecord> records)
        {
            decimal previousClose = 0;

            foreach (var record in records)
            {
                if (record.Close == 0)
                {
                    record.Close = previousClose;
                }
                else
                {
                    previousClose = record.Close;
                }
            }
        }
        public static List<TickRecord> CombineLists(List<TickRecord> futures, List<TickRecord> opts)
        {
            // Превратить список opts в словарь для быстрого поиска по дате
            Dictionary<DateTime, TickRecord> optsDict = opts.ToDictionary(o => o.DateTime);

            List<TickRecord> combinedList = new List<TickRecord>();

            // Идем по списку futures и добавляем соответствующие элементы из opts или пустые записи с нулями
            foreach (var future in futures)
            {
                if (optsDict.TryGetValue(future.DateTime, out var opt))
                {
                    combinedList.Add(opt);
                }
                else
                {
                    combinedList.Add(new TickRecord
                    {
                        Ticker = future.Ticker,
                        Per = future.Per,
                        DateTime = future.DateTime,
                        Open = 0,
                        High = 0,
                        Low = 0,
                        Close = 0,
                        Vol = 0
                    });
                }
            }

            return combinedList;
        }
        public static string SerializeSimpleTickRecords(List<SimpleTickRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            // Добавление заголовка
            sb.AppendLine("<Ticker>;<Date>;<Time>;<CloseOI>;<CloseDifference>;<ClosePrice>;<Volume>");

            // Добавление данных
            foreach (var record in records)
            {
                sb.AppendLine($"{record.Ticker};{record.Date};{record.Time};{record.CloseOI};{record.CloseDifference};{record.ClosePrice};{record.Volume}");
            }

            return sb.ToString();
        }
        public static List<TickRecord> ReadFile(string filePath)
        {
            var tickRecords = new List<TickRecord>();

            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines.Skip(1)) // Assuming first line is header
            {
                var values = line.Split(',');
                var tickRecord = new TickRecord
                {
                    Ticker = values[0],
                    Per = int.Parse(values[1]),
                    DateTime = DateTime.ParseExact(values[2] + values[3], "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                    Open = decimal.Parse(values[4], CultureInfo.InvariantCulture),
                    High = decimal.Parse(values[5], CultureInfo.InvariantCulture),
                    Low = decimal.Parse(values[6], CultureInfo.InvariantCulture),
                    Close = decimal.Parse(values[7], CultureInfo.InvariantCulture),
                    Vol = decimal.Parse(values[8], CultureInfo.InvariantCulture)
                };
                tickRecords.Add(tickRecord);
            }

            return tickRecords;
        }


        private static DateTime RoundToNearestInterval(DateTime dt, int intervalMinutes)
        {

            var totalMinutes = (int)dt.TimeOfDay.TotalMinutes;
            //if (totalMinutes < 540)                totalMinutes = 540;

            var roundedTotalMinutes = (totalMinutes / intervalMinutes) * intervalMinutes;
            return dt.Date.AddMinutes(roundedTotalMinutes);
        }


        public class OutputTickRecord
        {
            public decimal Oi { get; set; }
            public decimal Vol { get; set; }
            public decimal Price { get; set; }

            public override string ToString()
            {
                Func<decimal, string> tos = (z) => z == 0 ? "" : z.ToString();

                return $"{tos(Oi)};{tos(Vol)};{tos(Price)}";
            }
        }
        public static List<OutputTickRecord> CombineTables(List<TickRecord> table1, List<TickRecord> table2)
        {
            var res = new List<OutputTickRecord>();
            for (var i = 0; i < table1.Count; i++)
            {
                var t = new OutputTickRecord
                {
                    Oi = table1[i].Close != 0 ? (table1[i].Close / 2) : 0,
                    Vol = table2[i].Vol != 0 ? table2[i].Vol : 0,
                    Price = table2[i].Vol != 0 ? table2[i].Low : 0,
                };

                res.Add(t);
            }
            return res;

        }


        public static void RetainMatchingDateTimes(ref List<TickRecord> primaryList, List<TickRecord> comparisonList)
        {
            var comparisonDates = new HashSet<DateTime>(comparisonList.Select(record => record.DateTime));

            primaryList = primaryList
                .Where(record => comparisonDates.Contains(record.DateTime))
                .OrderBy(record => record.DateTime)
                .ToList();
        }

        public static Additional Adddata(List<TickRecord> futures, string opt, string d, decimal num, int per)
        {
            var pricedata = ReadFile(d + $@"{opt} {num}.txt");
            var pricedatagroupped = GroupTickRecords(pricedata, per);
            var oidata = ReadFile(d + $@"OI {opt} {num}.txt");
            var oidatagroupped = GroupTickRecords(oidata, per);


            RetainMatchingDateTimes(ref pricedatagroupped, oidatagroupped);

            var pricedatastrached = CombineLists(futures, pricedatagroupped);
            var oidatastratched = CombineLists(futures, oidatagroupped);
            FillCloseWithPrevious(oidatastratched);
            FillCloseWithPrevious(pricedatastrached);

            for (int i = 0; i < pricedatastrached.Count; i++)
            {
                if (i > 0 && pricedatastrached[i].Vol != 0)
                {
                    pricedatastrached[i].Vol = (oidatastratched[i].Close - oidatastratched[i - 1].Close) / 2;
                }
            }

            return new Additional
            {
                num = num,
                opt = opt,
                list = CombineTables(oidatastratched, pricedatastrached)
            };
        }

        public static List<Tuple<string, decimal>> ScanFilesInFolder(string folderPath)
        {
            // Проверяем, существует ли папка
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Папка не найдена: {folderPath}");
            }

            // Получаем имена файлов в папке
            var files = Directory.GetFiles(folderPath);

            // Регулярное выражение для поиска необходимых файлов
            var regex = new Regex(@"^(CALL|PUT) (\d+\,\d+|\d+)\.txt$", RegexOptions.IgnoreCase);

            // Создаем список для хранения результатов
            var result = new List<Tuple<string, decimal>>();

            // Проходимся по каждому файлу и проверяем его соответствие регулярному выражению
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var match = regex.Match(fileName);
                if (match.Success)
                {
                    string type = match.Groups[1].Value.ToUpper();
                    decimal number = decimal.Parse(match.Groups[2].Value);
                    result.Add(new Tuple<string, decimal>(type, number));
                }
            }

            return result;
        }

        public static string doall(string dirPath, string futpath, int per)
        {
            var futures = DataProcessor.ReadFile(futpath);

            futures = GroupTickRecordsFut(futures, per);

            var zzz = ScanFilesInFolder(dirPath);

            List<Additional> list = new List<Additional>();

            foreach (var z in zzz)
            {
                var xx1 = Adddata(futures, z.Item1, dirPath, z.Item2, per);
                list.Add(xx1);
            }

            list = list.OrderBy(x => x.opt).ThenBy(x => x.num).ToList();
            return SerializeTickRecords(futures, list);
        }

        public static string SerializeTickRecords(List<TickRecord> tickRecords, List<Additional> add)
        {
            StringBuilder sb = new StringBuilder();

            string s1 = ";;;;";
            for (var j = 0; add.Count > j; j++)
            {
                s1 += $";; {add[j].opt}{add[j].num} ;;";
            }
            s1 += "\r\n";
            sb.Append(s1);

            s1 = "ticker;period;date;time;price";
            for (var j = 0; add.Count > j; j++)
            {
                s1 += $";;  OI; VOL; PRICE";
            }
            s1 += "\r\n";
            sb.Append(s1);
            for (var i = 0; i < tickRecords.Count; i++)
            {
                var record = tickRecords[i];
                string date = record.DateTime.ToString("yyyyMMdd");
                string time = record.DateTime.ToString("HHmmss");

                var s = string.Format(CultureInfo.InvariantCulture,
                    "{0};{1};{2};{3};{4:F6}",
                    record.Ticker,
                    record.Per,
                    date,
                    time,
                    record.Close
                    );


                var allstr = add.Sum(addj => addj.list[i].Vol);// String.Join("", add.Select(addj => addj.list[i].Oi + addj.list[i].Vol + addj.list[i].Price));

                if ((allstr) == 0)
                    continue;

                for (var j = 0; add.Count > j; j++)
                {
                    s += $";;{add[j].list[i]}";
                }
                s += "\r\n";

                sb.Append(s);
            }

            return sb.ToString();
        }

        public static List<TickRecord> GroupTickRecordsFut(List<TickRecord> tickRecords, int newPeriod)
        {
            newPeriod = 1;

            var groupedRecords = new List<TickRecord>();

            var groupedByTicker = tickRecords.GroupBy(rec => RoundToNearestInterval(rec.DateTime, newPeriod));

            foreach (var tickerGroup in groupedByTicker)
            {
                var tickerRecords = tickerGroup.OrderBy(rec => rec.DateTime).ToList();

                for (int i = 0; i < tickerRecords.Count; i += newPeriod)
                {
                    var periodRecords = tickerRecords.Skip(i).Take(newPeriod).ToList();
                    if (periodRecords.Count == 0) continue;

                    var groupedRecord = new TickRecord
                    {
                        Ticker = periodRecords.First().Ticker,
                        Per = newPeriod,
                        DateTime = RoundToNearestInterval(periodRecords.First().DateTime, newPeriod),
                        Open = periodRecords.First().Open,
                        High = periodRecords.Max(r => r.High),
                        Low = periodRecords.Min(r => r.Low),
                        Close = periodRecords.Last().Close,
                        Vol = periodRecords.Sum(r => r.Vol)
                    };

                    groupedRecords.Add(groupedRecord);
                }
            }

            return groupedRecords;
        }

        public static List<TickRecord> GroupTickRecords(List<TickRecord> tickRecords, int newPeriod)
        {
            newPeriod = 1;
            var groupedRecords = new List<TickRecord>();

            var groupedByTicker = tickRecords.GroupBy(rec => RoundToNearestInterval(rec.DateTime, newPeriod));

            foreach (var tickerGroup in groupedByTicker)
            {
                var tickerRecords = tickerGroup.OrderBy(rec => rec.DateTime).ToList();

                for (int i = 0; i < tickerRecords.Count; i += newPeriod)
                {
                    var periodRecords = tickerRecords.Skip(i).Take(newPeriod).ToList();
                    if (periodRecords.Count == 0) continue;

                    var groupedRecord = new TickRecord
                    {
                        Ticker = periodRecords.First().Ticker,
                        Per = newPeriod,
                        DateTime = RoundToNearestInterval(periodRecords.First().DateTime, newPeriod),
                        Open = periodRecords.First().Open,
                        High = periodRecords.Max(r => r.High),
                        Low = periodRecords.Sum(x => x.Close * x.Vol) / periodRecords.Sum(x => x.Vol),
                        Close = periodRecords.Last().Close,
                        Vol = periodRecords.Sum(r => r.Vol)
                    };

                    groupedRecords.Add(groupedRecord);
                }
            }

            return groupedRecords;
        }
    }
}
