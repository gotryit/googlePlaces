//#define NET5

using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Configuration;

#if NET5
using System.Net.Http.Json;
#else
using System.Text.Json;
#endif

using static System.Console;


var optionIndex = 0;
var myText = "";

var lines = new List<string>();

var searchMapsUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/json";
var detailsMapsUrl = "https://maps.googleapis.com/maps/api/place/details/json";

/*
app.config

<configuration>
    <appSettings>
        <add key="api_key" value="" />
    </appSettings>
</configuration>
*/

var apiKey = ConfigurationManager.AppSettings["api_key"];
var randomSessionToken = System.Guid.NewGuid();
var language = "ro";

var client = new HttpClient();



Func<string> getSearchUri = () => $"{searchMapsUrl}?&key={apiKey}&sessiontoken={randomSessionToken}&language={language}&types=address&input={myText}";
Func<string, string> getDetailsUrl = (string placeId) => $"{detailsMapsUrl}?&key={apiKey}&sessiontoken={randomSessionToken}&language={language}&place_id={placeId}";

Action refreshConsole = () => {

    Clear();
    WriteLine($"Search address: {myText}");

    for (int i = 0; i < lines.Count; i++)
    {
        SetCursorPosition(0, i + 1);
        if (optionIndex == i)
        {
            WriteLine($"> {lines[i]}");
        }
        else
        {
            WriteLine($"  {lines[i]}");
        }
    }

    SetCursorPosition("Search address: ".Length + myText.Length, 0);
};

Action callSearchApi = async () => {

#if NET5
    var addressPrediction = await client.GetFromJsonAsync<AddressPrediction>(getSearchUri());
#else
    using var response = await client.GetAsync(getSearchUri(), HttpCompletionOption.ResponseHeadersRead);

    response.EnsureSuccessStatusCode();

    var addressPrediction = await JsonSerializer.DeserializeAsync<AddressPrediction>(await response.Content.ReadAsStreamAsync());
#endif

    lines.Clear();
    optionIndex = 0;

    foreach(var address in addressPrediction.Predictions)
    {
        lines.Add($"{address.Description} ({address.PlaceId})");
    }

    refreshConsole();
};

refreshConsole();
var myChar = ReadKey();

while(myChar.Key != ConsoleKey.Enter)
{
    var needRefresh = true;

    if (myChar.Key == ConsoleKey.Backspace)
    {
        if (myText.Length > 0) myText = myText[..^1];
    }
    else if (myChar.Key == ConsoleKey.UpArrow)
    {
        optionIndex = Math.Max(optionIndex - 1, 0);
        refreshConsole();

        needRefresh = false;
    }
    else if (myChar.Key == ConsoleKey.DownArrow)
    {
        optionIndex = Math.Min(optionIndex + 1, lines.Count - 1);
        refreshConsole();

        needRefresh = false;
    }
    else
    {
        myText += myChar.KeyChar;
    }

    if (myText.Length > 2)
    {
        if (needRefresh) callSearchApi();
    }
    else
    {
        lines.Clear();
        optionIndex = 0;
        refreshConsole();
    }

    myChar = ReadKey();
}

if (lines.Count > 0)
{
    Clear();
    var placeId = lines[optionIndex][(lines[optionIndex].IndexOf('(') + 1)..^1];
    

#if NET5
    var result = await client.GetFromJsonAsync<Result>(getDetailsUrl(placeId));
#else
    using var response = await client.GetAsync(getDetailsUrl(placeId), HttpCompletionOption.ResponseHeadersRead);

    response.EnsureSuccessStatusCode();

    var result = await JsonSerializer.DeserializeAsync<Result>(await response.Content.ReadAsStreamAsync());
#endif

    foreach(var component in result.AddressDetails.AddressComponents)
    {
        if (component.Types.Contains("locality"))
        {
            WriteLine($"City: {component.LongName}");
        }

        if (component.Types.Contains("administrative_area_level_1"))
        {
            WriteLine($"County: {component.LongName} ({component.ShortName})");
        }

        if (component.Types.Contains("country"))
        {
            WriteLine($"Country: {component.LongName} - {component.ShortName}");
        }

        if (component.Types.Contains("postal_code"))
        {
            WriteLine($"ZipCode: {component.LongName}");
        }

        if (component.Types.Contains("route"))
        {
            WriteLine($"Street: {component.LongName}");
        }

        if (component.Types.Contains("street_number"))
        {
            WriteLine($"Street No: {component.LongName}");
        }
    }
}

public class AddressPrediction
{
    [JsonPropertyName("predictions")] public List<Prediction> Predictions { get; set; }
}
public class Prediction
{
    [JsonPropertyName("description")] public string Description { get; set; }
    [JsonPropertyName("place_id")] public string PlaceId { get; set; }    
}

public class Result
{
    [JsonPropertyName("result")] public AddressDetails AddressDetails { get; set; }
}

public class AddressDetails
{
    [JsonPropertyName("address_components")] public List<AddressComponent> AddressComponents { get; set; }
}

public class AddressComponent
{
    [JsonPropertyName("long_name")] public string LongName { get; set; }
    [JsonPropertyName("short_name")] public string ShortName { get; set; }
    [JsonPropertyName("types")] public List<string> Types { get; set; }
}

