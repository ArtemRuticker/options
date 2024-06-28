using Newtonsoft.Json;
using System.Collections.Generic;

public class PortfolioItem
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("open")]
    public int Open { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("expiration")]
    public string Expiration { get; set; }

    [JsonProperty("price")]
    public int Price { get; set; }

    [JsonProperty("assetPrice")]
    public int AssetPrice { get; set; }

    [JsonProperty("optionType")]
    public string OptionType { get; set; }

    [JsonProperty("strike")]
    public int Strike { get; set; }

    [JsonProperty("qty")]
    public int Quantity { get; set; }
}

public class PortfolioData
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("filter")]
    public string Filter { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("portfolio")]
    public List<PortfolioItem> Portfolio { get; set; }

    [JsonProperty("assetPrice")]
    public int AssetPrice { get; set; }

    [JsonProperty("updatePeriod")]
    public int UpdatePeriod { get; set; }
}


class jsonSer
{




}

/*
class Program
{
    static void Main(string[] args)
    {
        // Sample JSON data
        string jsonData = @"{
            'name': 'SiM4_20240620_122400',
            'filter': 'custom',
            'description': 'Создан 27.06.24',
            'portfolio': [
                {
                    'type': 'option',
                    'open': 6189,
                    'code': '&nbsp;',
                    'expiration': '18.07.24',
                    'price': 6189,
                    'assetPrice': 85993,
                    'optionType': 'call',
                    'strike': 80000,
                    'qty': 33
                },
                {
                    'type': 'option',
                    'open': 5284,
                    'code': '&nbsp;',
                    'expiration': '18.07.24',
                    'price': 5284,
                    'assetPrice': 85993,
                    'optionType': 'call',
                    'strike': 81000,
                    'qty': 11
                },
                {
                    'type': 'option',
                    'open': 4418,
                    'code': '&nbsp;',
                    'expiration': '18.07.24',
                    'price': 4418,
                    'assetPrice': 85993,
                    'optionType': 'call',
                    'strike': 82000,
                    'qty': 18
                },
                {
                    'type': 'option',
                    'open': 89,
                    'code': '&nbsp;',
                    'expiration': '18.07.24',
                    'price': 89,
                    'assetPrice': 85993,
                    'optionType': 'put',
                    'strike': 71000,
                    'qty': 130
                }
            ],
            'assetPrice': 85993,
            'updatePeriod': 1200000
        }".Replace('\'', '\"');

        // Deserialize JSON data to PortfolioData object
        PortfolioData portfolioData = JsonConvert.DeserializeObject<PortfolioData>(jsonData);

        // Serialize PortfolioData object back to JSON
        string serializedJson = JsonConvert.SerializeObject(portfolioData, Formatting.Indented);
        Console.WriteLine(serializedJson);
    }*/

