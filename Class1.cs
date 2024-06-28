using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace Options
{
    public class OutputTickRecord
    {
        public decimal Oi { get; set; }
        public decimal Vol { get; set; }
        public decimal Price { get; set; }
        public decimal MP { get; set; } // New field for cumulative average price

        public override string ToString()
        {
            Func<decimal, string> tos = (z) => z == 0 ? "" : z.ToString();
            return $"{tos(Oi)};{tos(Vol)};{tos(Price)};{tos(MP)}";
        }
    }
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

    public class DataProcessor
    {
        public List<TickRecord> futures { get; private set; }
        public List<Additional> list { get; private set; }

        public void FillCloseWithPrevious(List<TickRecord> records)
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

        public List<string> errors = new List<string>() { "name;opti time;closest futtime;volume;\r\n" };
        static Dictionary<DateTime, TickRecord> futdic = null;

        public List<TickRecord> CombineLists(List<TickRecord> futures, List<TickRecord> opts, string optname, decimal num)
        {
            // Превратить список opts в словарь для быстрого поиска по дате

            if (futdic == null)
                futdic = futures.ToDictionary(o => o.DateTime);


            Dictionary<DateTime, TickRecord> optsDict = opts.ToDictionary(o => o.DateTime);

            List<TickRecord> combinedList = new List<TickRecord>();


            foreach (var opt in opts)
            {
                if (!futdic.ContainsKey(opt.DateTime))
                {
                    var closest1 = futdic.Values.Select(x => new { dt = x.DateTime, order = Math.Abs((x.DateTime - opt.DateTime).Ticks) }).OrderBy(x => x.order).FirstOrDefault();
                    var closest = closest1 == null ? "" : closest1.dt.ToString();

                    if (optname.Contains("OI"))
                        errors.Add($"{optname} {num};{opt.DateTime};{closest};{opt.Close / 2};\r\n");

                }
            }

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

        public string SerializeSimpleTickRecords(List<SimpleTickRecord> records)
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

        public List<TickRecord> ReadFile(string filePath)
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

        private DateTime RoundToNearestInterval(DateTime dt, int intervalMinutes)
        {
            var totalMinutes = (int)dt.TimeOfDay.TotalMinutes;
            //if (totalMinutes < 540) totalMinutes = 540;

            var roundedTotalMinutes = (totalMinutes / intervalMinutes) * intervalMinutes;
            return dt.Date.AddMinutes(roundedTotalMinutes);
        }



        public List<OutputTickRecord> CombineTables(List<TickRecord> table1, List<TickRecord> table2)
        {
            var res = new List<OutputTickRecord>();
            decimal cumulativeVolume = 0;
            decimal cumulativePriceVolume = 0;

            for (var i = 0; i < table1.Count; i++)
            {
                cumulativeVolume += table2[i].Vol;
                cumulativePriceVolume += table2[i].Low * table2[i].Vol;

                var t = new OutputTickRecord
                {
                    Oi = table1[i].Close != 0 ? (table1[i].Close / 2) : 0,
                    Vol = table2[i].Vol != 0 ? table2[i].Vol : 0,
                    Price = table2[i].Vol != 0 ? table2[i].Low : 0,
                    MP = cumulativeVolume != 0 ? cumulativePriceVolume / cumulativeVolume : 0 // Calculate cumulative average price
                };

                res.Add(t);
            }

            return res;
        }

        public void RetainMatchingDateTimes(ref List<TickRecord> primaryList, List<TickRecord> comparisonList)
        {
            var comparisonDates = new HashSet<DateTime>(comparisonList.Select(record => record.DateTime));

            primaryList = primaryList
                .Where(record => comparisonDates.Contains(record.DateTime))
                .OrderBy(record => record.DateTime)
                .ToList();
        }

        public Additional Adddata(List<TickRecord> futures, string opt, string d, decimal num, int per)
        {
            var pricedata = ReadFile(d + $@"{opt} {num}.txt");
            var pricedatagroupped = GroupTickRecords(pricedata, per);
            var oidata = ReadFile(d + $@"OI {opt} {num}.txt");
            var oidatagroupped = GroupTickRecords(oidata, per);

            RetainMatchingDateTimes(ref pricedatagroupped, oidatagroupped);

            var pricedatastrached = CombineLists(futures, pricedatagroupped, opt, num);
            var oidatastratched = CombineLists(futures, oidatagroupped, "OI" + opt, num);
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


        public List<Tuple<string, decimal>> ScanFilesInFolder(string folderPath)
        {
            // Проверяем, существует ли папка
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Папка не найдена: {folderPath}");
            }

            // Получаем имена файлов в папке
            var files = Directory.GetFiles(folderPath);

            // Регулярное выражение для поиска необходимых файлов
            var regex = new Regex(@"^(CALL|PUT|OI CALL|OI PUT) (\d+,\d+|\d+)\.txt$", RegexOptions.IgnoreCase);

            // Создаем словарь для хранения результатов
            var fileDict = new Dictionary<string, HashSet<decimal>>();

            // Проходимся по каждому файлу и проверяем его соответствие регулярному выражению
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var match = regex.Match(fileName);
                if (match.Success)
                {
                    string type = match.Groups[1].Value.ToUpper();
                    decimal number = decimal.Parse(match.Groups[2].Value);

                    if (!fileDict.ContainsKey(type))
                    {
                        fileDict[type] = new HashSet<decimal>();
                    }

                    fileDict[type].Add(number);
                }
            }

            // Проверка наличия соответствующих пар файлов
            var errors = new List<string>();

            foreach (var type in new[] { "CALL", "PUT" })
            {
                var oiType = "OI " + type;
                if (fileDict.ContainsKey(type) && fileDict.ContainsKey(oiType))
                {
                    var missingOITypes = fileDict[type].Except(fileDict[oiType]).ToList();
                    var missingTypes = fileDict[oiType].Except(fileDict[type]).ToList();

                    if (missingOITypes.Any())
                    {
                        errors.Add($"Не хватает OI {type} файлов для номеров: {string.Join(", ", missingOITypes)}");
                    }

                    if (missingTypes.Any())
                    {
                        errors.Add($"Не хватает {type} файлов для номеров: {string.Join(", ", missingTypes)}");
                    }
                }
                else
                {
                    if (fileDict.ContainsKey(type) && !fileDict.ContainsKey(oiType))
                    {
                        errors.Add($"Не хватает всех OI {type} файлов.");
                    }
                    if (!fileDict.ContainsKey(type) && fileDict.ContainsKey(oiType))
                    {
                        errors.Add($"Не хватает всех {type} файлов.");
                    }
                }
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join("; ", errors));
            }

            // Преобразуем результаты в список Tuples
            var result = new List<Tuple<string, decimal>>();
            foreach (var kvp in fileDict.Where(x => !x.Key.StartsWith("OI")))
            {
                foreach (var number in kvp.Value)
                {
                    result.Add(new Tuple<string, decimal>(kvp.Key, number));
                }
            }

            return result;
        }


        public void doall(string dirPath, string futpath, int per)
        {
            futures = ReadFile(futpath);
            futures = GroupTickRecordsFut(futures, per);

            var zzz = ScanFilesInFolder(dirPath);

            list = new List<Additional>();

            foreach (var z in zzz)
            {
                var xx1 = Adddata(futures, z.Item1, dirPath, z.Item2, per);
                list.Add(xx1);
            }



            list = list.OrderBy(x => x.opt).ThenBy(x => x.num).ToList();


            //   return SerializeTickRecords(futures, list);
        }


        public int foundNum(string d, string t)
        {
            for (var i = 0; i < futures.Count; i++)
            {
                var record = futures[i];
                string date = record.DateTime.ToString("yyyyMMdd");
                string time = record.DateTime.ToString("HHmmss");
                if (time == t && date == d)
                    return i;
            }
            throw new Exception($"Не найдена строка со временем {d} {t} ");
        }

        public PortfolioData json(int line, string exp)

        {
            TickRecord tickRecords = futures[line];


            PortfolioData pp
                = new PortfolioData()
                {
                    Name = $"{tickRecords.Ticker} , {tickRecords.DateTime.ToShortDateString()} , {tickRecords.DateTime.ToShortTimeString()}",
                    Filter = "custom",
                    Description = $"Создан {DateTime.Now.Date.ToShortDateString()}",
                    AssetPrice = tickRecords.Close,


                };


            pp.Portfolio = list
                 .Where(x => x.list[line].Oi > 0)
                .Select(x =>
                  new PortfolioItem()
                  {
                      Type = "option",
                      Code = "&amp;nbsp;",
                      AssetPrice = tickRecords.Close,
                      Expiration = exp,
                      Price = x.list[line].MP,
                      Open = x.list[line].MP,
                      OptionType = x.opt.ToLower(),
                      Strike = x.num,
                      Quantity = (int)x.list[line].Oi
                  }).ToList();

            return pp;

        }

        public string SerializeTickRecords()
        {

            return SerializeTickRecords(futures, list);
        }

        public string SerializeTickRecords(List<TickRecord> tickRecords, List<Additional> add)
        {


            StringBuilder sb = new StringBuilder();

            string s1 = ";;;;";
            for (var j = 0; add.Count > j; j++)
            {
                s1 += $";; {add[j].opt}{add[j].num} ;;;";
            }
            s1 += "\r\n";
            sb.Append(s1);

            s1 = "ticker;period;date;time;price";
            for (var j = 0; add.Count > j; j++)
            {
                s1 += $";;  OI; VOL; PRICE; MP";
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


                /// yyy

                for (var j = 0; add.Count > j; j++)
                {
                    s += $";;{add[j].list[i]}";
                }
                s += "\r\n";

                sb.Append(s);
            }

            return sb.ToString();
        }

        public List<TickRecord> GroupTickRecordsFut(List<TickRecord> tickRecords, int newPeriod)
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

        public List<TickRecord> GroupTickRecords(List<TickRecord> tickRecords, int newPeriod)
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
