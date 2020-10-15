﻿using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using static System.Console;


Console.Clear();

var cancelSource = new CancellationTokenSource();

var optionIndex = 0;
var myText = "";

var lines = new List<string>();

var searchMapsUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/json";
var detailsMapsUrl = "https://maps.googleapis.com/maps/api/place/details/json";

var centerOfRomania = "45.9432,24.9668";
var radiusOfRomania = "400000";

var apiKey = "";
var randomSessionToken = System.Guid.NewGuid();

Func<string> getSearchUri = () => $"{searchMapsUrl}?&key={apiKey}&sessiontoken={randomSessionToken}&types=(cities)&input={myText}&location={centerOfRomania}&radius={radiusOfRomania}";
Func<string, string> getDetailsUrl = (string placeId) => $"{detailsMapsUrl}?&key={apiKey}&sessiontoken={randomSessionToken}&language=ro&place_id={placeId}";

Action refreshConsole = () => {
    Console.SetCursorPosition(0, 0);

    Write("\r" + new string(' ', Console.WindowWidth) + "\r");    
    WriteLine(myText);

    for (int i = 0; i < lines.Count; i++)
    {
        Console.SetCursorPosition(0, i + 1);
        if (optionIndex == i)
        {
            WriteLine($"> {lines[i]}");
        }
        else
        {
            WriteLine($"  {lines[i]}");
        }
    }

    Console.SetCursorPosition(myText.Length, 0);
};

Action callSearchApi = async () => {

    var googlePlacesClient = new HttpClient();

    try
    {
        lines.Clear();

        var response = await googlePlacesClient.GetAsync(getSearchUri(), cancelSource.Token);

        if (response.IsSuccessStatusCode)
        {
            var citiesPrediction =  JsonSerializer.Deserialize<CityPredictions>(await response.Content.ReadAsStringAsync());

            foreach(var city in citiesPrediction.Predictions)
            {
                lines.Add($"{city.Decription} ({city.PlaceId})");
            }

            refreshConsole();
        }
    }
    catch(OperationCanceledException)
    {
        
    }
};

refreshConsole();
var myChar = ReadKey();

while(myChar.Key != ConsoleKey.Enter)
{
    if (myChar.Key == ConsoleKey.Backspace)
    {
        if (myText.Length > 0) myText = myText[..^1];
    }
    else if (myChar.Key == ConsoleKey.UpArrow)
    {
        optionIndex = Math.Max(optionIndex - 1, 0);
        refreshConsole();
    }
    else if (myChar.Key == ConsoleKey.DownArrow)
    {
        optionIndex = Math.Min(optionIndex + 1, lines.Count - 1);
        refreshConsole();
    }
    else
    {
        myText += myChar.KeyChar;
    }

    Write("\r" + new string(' ', Console.WindowWidth) + "\r");
    Write(myText);

    if (myText.Length > 3)
    {
        cancelSource.Cancel();
        cancelSource = new CancellationTokenSource();
        Task.Run(callSearchApi);
    }

    myChar = ReadKey();
}

if (lines.Count > 0)
{
    Console.Clear();
    var placeId = lines[optionIndex][(lines[optionIndex].IndexOf('(') + 1)..^1];

    var googlePlacesClient = new HttpClient();

    var response = await googlePlacesClient.GetAsync(getDetailsUrl(placeId));

    if (response.IsSuccessStatusCode)
    {
        var cityDetail =  JsonSerializer.Deserialize<Result>(await response.Content.ReadAsStringAsync()).CityDetails;

        foreach(var component in cityDetail.AddressComponents)
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
        }
    }
}

public class CityPredictions
{
    [JsonPropertyName("predictions")] public List<Prediction> Predictions { get; set; }
}
public class Prediction
{
    [JsonPropertyName("description")] public string Decription { get; set; }
    [JsonPropertyName("place_id")] public string PlaceId { get; set; }    
}

public class Result
{
    [JsonPropertyName("result")] public CityDetails CityDetails { get; set; }
}

public class CityDetails
{
    [JsonPropertyName("address_components")] public List<AddressComponent> AddressComponents { get; set; }
}

public class AddressComponent
{
    [JsonPropertyName("long_name")] public string LongName { get; set; }
    [JsonPropertyName("short_name")] public string ShortName { get; set; }
    [JsonPropertyName("types")] public List<string> Types { get; set; }
}

