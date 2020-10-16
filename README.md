```javascript
@search_maps_url = https://maps.googleapis.com/maps/api/place/autocomplete/json
@details_maps_url = https://maps.googleapis.com/maps/api/place/details/json

@api_key = 
@random_session_token = 4b8080db-a537-4cd4-91bb-9835635d443a
@bucuresti_place_id = ChIJT608vzr5sUARKKacfOMyBqw
```

```javascript
GET {{search_maps_url}}
    ?&key={{api_key}}
    &sessiontoken={{random_session_token}}
    &types=(cities)
    &location=45.9432,24.9668
    &radius=400000
    &input=bucur
```

```javascript
GET {{details_maps_url}}
    ?&key={{api_key}}
    &sessiontoken={{random_session_token}}
    &language=ro
    &place_id={{bucuresti_place_id}}
```

- session token is generated by the client. session ends when details endpoint is called. google recommends to use 4UUID
- location and radius are nice to have
- language for details can be read from phone settings

Notes

- measure country radius

  https://www.mapsdirections.info/en/measure-map-radius/

- get center of country location

  https://developers.google.com/public-data/docs/canonical/countries_csv


Best practices for calling APIs:
- **use** IHttpClientFactory if you can (.net core container & .net core >= 2.1)
- **do not** instantiate new HttpClient for each call as the connection socket remains open even after dispose
- **use** streams for content read and deserialization for optimal memory usage
- start reading content stream early, right after the headers are read, using HttpCompletionOption.ResponseHeadersRead
- **use** System.Net.Http.Json if you can (.net 5)



