module TwitterApi.Engine


open System
open System.Collections.Generic
open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Templating
open System.Data.SQLite 


let db_init() =
    let databaseFilename = "sample.sqlite"
    let connectionString = sprintf "Data Source=%s;Version=3;" databaseFilename
    let connection = new SQLiteConnection(connectionString)
    connection.Open()

    let structureSql =
        "create table if not exists RegistrationInfo (" +
        "userId int," +
        "uname varchar(30)," +
        "password varchar(30)) " 

    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore
    printfn "created Database"



/// The types used by this application.
module Model =

    /// Data about a person. Used both for storage and JSON parsing/writing.
    type PersonData =
        {
            id: int
            firstName: string
            lastName: string
            born: System.DateTime
            /// Since this is an option, this field is only present in JSON for Some value.
            died: option<System.DateTime>
        }

    /// Data about a person. Used both for storage and JSON parsing/writing.
    type UserData =
        {
            id: int
            uname : string
            pwd: string
        }


    type ResponseData =
        {
            text: string
        }

    /// The type of REST API endpoints.
    /// This defines the set of requests accepted by our API.
    type ApiEndPoint =

        /// Accepts POST requests to /people with PersonData as JSON body
        | [<EndPoint "POST /register-user"; Json "userData">]
            CreateUser of userData: UserData

        | [<EndPoint "GET /login">]
            GetLogin of uname: string

        /// Accepts GET requests to /people
        | [<EndPoint "GET /user">]
            GetUser

        /// Accepts GET requests to /people
        | [<EndPoint "GET /people">]
            GetPeople

        /// Accepts GET requests to /people/{id}
        | [<EndPoint "GET /people">]
            GetPerson of id: int

        /// Accepts POST requests to /people with PersonData as JSON body
        | [<EndPoint "POST /people"; Json "personData">]
            CreatePerson of personData: PersonData

        /// Accepts PUT requests to /people with PersonData as JSON body
        | [<EndPoint "PUT /people"; Json "personData">]
            EditPerson of personData: PersonData

        /// Accepts DELETE requests to /people/{id}
        | [<EndPoint "DELETE /people">]
            DeletePerson of id: int

    /// The type of all endpoints for the application.
    type EndPoint =
        
        /// Accepts requests to /
        | [<EndPoint "/">] Home

        /// Accepts requests to /api/...
        | [<EndPoint "/api">] Api of Cors<ApiEndPoint>

    /// Error result value.
    type Error = { error : string }

    /// Alias representing the success or failure of an operation.
    /// The Ok case contains a success value to return as JSON.
    /// The Error case contains an HTTP status and a JSON error to return.
    type ApiResult<'T> = Result<'T, Http.Status * Error>

    /// Result value for CreatePerson.
    type Id = { id : int }

open Model

/// This module implements the back-end of the application.
/// It's a CRUD application maintaining a basic in-memory database of people.
module Backend =

    let databaseFilename = "sample.sqlite"
    let connectionString = sprintf "Data Source=%s;Version=3;" databaseFilename
    let connection = new SQLiteConnection(connectionString)
    connection.Open()
    /// The people database.
    /// This is a dummy implementation, of course; a real-world application
    /// would go to an actual database.
    let private people = new Dictionary<int, PersonData>()

    let private user = new Dictionary<int, UserData>()
    /// The highest id used so far, incremented each time a person is POSTed.
    let private lastId = ref 0

    let personNotFound() : ApiResult<'T> =
        Error (Http.Status.NotFound, { error = "Person not found." })

    let CreateUser (data: UserData) : ApiResult<Id> = 
            incr lastId
            let insertSql = 
                    "insert into RegistrationInfo( userId, uname, password) " + 
                    "values (@userId, @uname, @password)"

            use command = new SQLiteCommand(insertSql, connection)
            command.Parameters.AddWithValue("@userId", lastId) |> ignore
            command.Parameters.AddWithValue("@uname", data.uname) |> ignore
            command.Parameters.AddWithValue("@password", data.pwd) |> ignore
            command.ExecuteNonQuery() |> ignore
            Ok { id = !lastId }
            
            
    let GetLogin (uname: string) : ApiResult<ResponseData> =
        let selectSql = """select  uname from RegistrationInfo where uname = " """ + uname + """ " """
        let selectCommand = new SQLiteCommand(selectSql, connection)
        let reader = selectCommand.ExecuteReader()
        let mutable isExistsLogin = false
        while reader.Read() do
            isExistsLogin <- true

        if isExistsLogin then   
           Ok { text = "True" }
        else
           Ok { text = "False" }

    let GetUser () : ApiResult<UserData[]> =
        lock user <| fun () ->
            user
            |> Seq.map (fun (KeyValue(_, u)) -> u)
            |> Array.ofSeq
            |> Ok

    let GetPeople () : ApiResult<PersonData[]> =
        lock people <| fun () ->
            people
            |> Seq.map (fun (KeyValue(_, person)) -> person)
            |> Array.ofSeq
            |> Ok

    let GetPerson (id: int) : ApiResult<PersonData> =
        lock people <| fun () ->
            match people.TryGetValue(id) with
            | true, person -> Ok person
            | false, _ -> personNotFound()

    let CreatePerson (data: PersonData) : ApiResult<Id> =
        lock people <| fun () ->
            incr lastId
            people.[!lastId] <- { data with id = !lastId }
            Ok { id = !lastId }

    let EditPerson (data: PersonData) : ApiResult<Id> =
        lock people <| fun () ->
            match people.TryGetValue(data.id) with
            | true, _ ->
                people.[data.id] <- data
                Ok { id = data.id }
            | false, _ -> personNotFound()

    let DeletePerson (id: int) : ApiResult<Id> =
        lock people <| fun () ->
            match people.TryGetValue(id) with
            | true, _ ->
                people.Remove(id) |> ignore
                Ok { id = id }
            | false, _ -> personNotFound()

    // On application startup, pre-fill the database with a few people.
    do List.iter (CreatePerson >> ignore) [
        { id = 0
          firstName = "Alonzo"
          lastName = "Church"
          born = DateTime(1903, 6, 14)
          died = Some(DateTime(1995, 8, 11)) }
        { id = 0
          firstName = "Alan"
          lastName = "Turing"
          born = DateTime(1912, 6, 23)
          died = Some(DateTime(1954, 6, 7)) }
        { id = 0
          firstName = "Bertrand"
          lastName = "Russell"
          born = DateTime(1872, 5, 18)
          died = Some(DateTime(1970, 2, 2)) }
        { id = 0
          firstName = "Noam"
          lastName = "Chomsky"
          born = DateTime(1928, 12, 7)
          died = None }
    ]

/// The server side website, tying everything together.
module Site =
    open WebSharper.UI
    open WebSharper.UI.Html
    open WebSharper.UI.Server

    /// Helper function to convert our internal ApiResult type into WebSharper Content.
    let JsonContent (result: ApiResult<'T>) : Async<Content<EndPoint>> =
        match result with
        | Ok value ->
            Content.Json value
        | Error (status, error) ->
            Content.Json error
            |> Content.SetStatus status
        |> Content.WithContentType "application/json"

    /// Respond to an ApiEndPoint by calling the corresponding backend function
    /// and converting the result into Content.
    let ApiContent (ep: ApiEndPoint) : Async<Content<EndPoint>> =
        match ep with
        | CreateUser userData ->
            JsonContent (Backend.CreateUser userData)
        | GetLogin uname -> 
            JsonContent (Backend.GetLogin uname)
        | GetUser ->
            JsonContent (Backend.GetUser ())
        | GetPeople ->
            JsonContent (Backend.GetPeople ())
        | GetPerson id ->
            JsonContent (Backend.GetPerson id)
        | CreatePerson personData ->
            JsonContent (Backend.CreatePerson personData)
        | EditPerson personData ->
            JsonContent (Backend.EditPerson personData)
        | DeletePerson id ->
            JsonContent (Backend.DeletePerson id)



    /// A simple HTML home page.
    let HomePage (ctx: Context<EndPoint>) : Async<Content<EndPoint>> =
        // Type-safely creates the URI: "/api/people/1"
        let person1Link = ctx.Link (Api (Cors.Of (GetPerson 1)))
        Content.Page(
            Body = [
                p [] [text "API is running."]
                p [] [
                    text "Try querying: "
                    a [attr.href person1Link] [text person1Link]
                ]
            ]
        )

    type IndexTemplate = Template<"login.html", clientLoad = ClientLoad.FromDocument>

    /// The Sitelet parses requests into EndPoint values
    /// and dispatches them to the content function.
    let Main corsAllowedOrigins : Sitelet<EndPoint> =

        db_init()
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | Home -> HomePage ctx
            
            | Api api ->
                Content.Cors api (fun allows ->
                    { allows with
                        Origins = corsAllowedOrigins
                        Headers = ["Content-Type"]
                    }
                ) ApiContent
        )
