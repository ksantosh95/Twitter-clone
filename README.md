# A simple WebSharper-based REST API

This simple micro-service provides a CRUD interface for a database of people.

To keep things simple, the data is stored in a simple dictionary.

Here is an example HTTP session demonstrating the implemented API:

```
Request:                              | Response:
--------------------------------------+-----------------------------------------
GET /api/people/42                    | 200 OK
                                      |
                                      | { "id": 42,
                                      |   "firstName": "Alan",
                                      |   "lastName": "Turing",
                                      |   "born": "1912-06-23",
                                      |   "died": "1954-06-07" }
--------------------------------------+-----------------------------------------
GET /api/people                       | 200 OK
                                      |
                                      | [
                                      |   { "id": 1,
                                      |     "firstName": "Alonzo",
                                      |     "lastName": "Church",
                                      |     "born": "1903-6-14",
                                      |     "died": "1995-8-11" },
                                      |   // ...
                                      | ]
--------------------------------------+-----------------------------------------
POST /api/people                      | 200 OK
                                      |
{ "id": 0,                            | { "id": 43 }
  "firstName": "Noam",                |
  "lastName": "Chomsky",              |
  "born": "1928-12-07" }              |
--------------------------------------+-----------------------------------------
PUT /api/people                       | 404 Not Found
                                      |
{ "id": 44,                           | { "error": "Person not found." }
  "firstName": "Bertrand",            |
  "lastName": "Russell",              |
  "born": "1872-05-18",               |
  "died": "1970-02-02" }              |
--------------------------------------+-----------------------------------------
DELETE /api/people/43                 | 200 OK
                                      |
                                      | { "id": 43 }
```

## Building and starting the application

Simply run the following commands:

```
cd src
dotnet run
```

## Code walkthrough

The API is implemented in [App.fs](src/App.fs).

- The module `Model` contains the types used by our application:
    - `EndPoint` and `ApiEndPoint` together describe the endpoints of the API. They describe the accepted URLs and JSON bodies.
    - `PersonData`, `Id`, `Error` and `ApiResult` describe the JSON data accepted and returned by the API.

- The module `Backend` contains the back-end of the application:
    - A global dictionary which plays the role of the database.
    - Functions to create, get, edit and delete a person from this dictionary.

- The module `Site` ties everything together. It creates a `MultiPage` site which parses `EndPoint`s, calls the corresponding `Backend` functions, and returns the resulting value as HTTP JSON content.

## Play along

There are plenty of ways you can extend this application to try your hand at WebSharper sitelets. Here are some suggestions:

- Add pagination to `GET /api/people`.

    For this, you'll probably want to use query parameters, such as `GET /api/people?pageId=3`. You can do so using the `Query` attribute on your endpoint:
    
    ```fsharp
    type ApiEndPoint =
        | [<EndPoint "GET /api/people"; Query "pageId">]
            GetPeople of pageId: option<int>
    ```

- Add a `PATCH /api/people/{id}` which allows setting only some fields of a person (as opposed to `PUT` which sets all fields).

    You'll need to create a new record type, which will be similar to `Person` except that all its fields should be optional.

## Happy coding!
