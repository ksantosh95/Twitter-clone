module TwitterApi.Engine


open System
open System.Collections.Generic
open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Templating
open System.Data.SQLite 


let mutable userIdNameMap = Map.empty

let db_init() =

    // if (System.IO.File.Exists("sample.sqlite")) then
    //         System.IO.File.Delete("sample.sqlite")

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

    let structureSql =
            "create table if not exists TweetsInfo (" +
            "tweetId varchar(50)," +
            "msg varchar(50)," +
            "userId int)" 

    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore

    let structureSql =
            "create table if not exists HashtagInfo (" +
            "tweetId varchar(50)," +
            "hashtag varchar(50))" 

    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore

    let structureSql =
            "create table if not exists MentionsInfo (" +
            "tweetId varchar(50)," +
            "uname varchar(50)," +
            "senderUserId int)" 
    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore

    let structureSql =
            "create table if not exists SubscribeInfo (" +
            "userId int," + 
            "subscribeTo int)" 
    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore

    printfn "created Database"



/// The types used by this application.
module Model =

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


    type TweetData =
        {
            text: string
            userid: int
        }

    type SubscribeData =
        {
            uname: string
            subscribeTo: string
        }

    type TweetFetch = 
        {
            text: string
            sender: string 
        }

    /// The type of REST API endpoints.
    /// This defines the set of requests accepted by our API.
    type ApiEndPoint =

        /// Accepts POST requests to /people with PersonData as JSON body
        | [<EndPoint "POST /register-user"; Json "userData">]
            CreateUser of userData: UserData

        | [<EndPoint "GET /login">]
            GetLogin of uname: string

        | [<EndPoint "POST /new-tweet"; Json "tweetData">]
            NewTweet of tweetData: TweetData

        | [<EndPoint "POST /subscribe"; Json "subscribeData">]
            SubscribeUser of subscribeData: SubscribeData

        | [<EndPoint "GET /mention">]
            GetMentionTweets of uname: string

        | [<EndPoint "GET /subscribed-tweets">]
            GetSubscribedTweets of uname: string

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

    let getUserId(uname:string) = 
        let userid = userIdNameMap.[uname]
        userid

    let parseTweet (tweet:string) =
            let mutable hashtags = []
            let mutable mentions = []
            let words = tweet.Split ' '
            for word in words do
                if word.StartsWith("#") then
                    hashtags <- hashtags @ [word.[1..]]
                if word.StartsWith("@") then
                    mentions <- mentions @ [word.[1..]]
            ( List.toArray(hashtags),mentions |> List.toArray)

    //USER REGISTRATION
    let CreateUser (data: UserData) : ApiResult<Id> = 
            incr lastId
            let insertSql = 
                    "insert into RegistrationInfo( userId, uname, password) " + 
                    "values (@userId, @uname, @password)"

            use command = new SQLiteCommand(insertSql, connection)
            command.Parameters.AddWithValue("@userId", !lastId) |> ignore
            command.Parameters.AddWithValue("@uname", data.uname) |> ignore
            command.Parameters.AddWithValue("@password", data.pwd) |> ignore
            command.ExecuteNonQuery() |> ignore
            let temp = data.uname.ToString()
            userIdNameMap <- Map.add temp !lastId userIdNameMap
            Ok { id = !lastId }
            
    //USER LOGIN   
    let GetLogin (uname: string) : ApiResult<TweetData> =
        let temp = "\""+uname+"\"" 
        let selectSql = """select  uname from RegistrationInfo where uname = """ + temp
        let selectCommand = new SQLiteCommand(selectSql, connection)
        let reader = selectCommand.ExecuteReader()
        let mutable isExistsLogin = false
        while reader.Read() do
            isExistsLogin <- true

        if isExistsLogin then   
           Ok { text = "True"  
                userid = userIdNameMap.[uname] }
        else
           
           Ok { text = "False" 
                userid = 0 }

    let WriteHashtag (tweetId: string, hashtag: string) =
        let insertSql = 
                        "insert into HashtagInfo( tweetId, hashtag) " + 
                        "values (@tweetId, @hashtag)"
        use command = new SQLiteCommand(insertSql, connection)
        command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
        command.Parameters.AddWithValue("@hashtag", hashtag) |> ignore
        command.ExecuteNonQuery() |> ignore

    let WriteMention (tweetId: string, uname: string, senderUserId : int) =
        let insertSql = 
                        "insert into MentionsInfo( tweetId, uname, senderUserId) " + 
                        "values (@tweetId, @uname, @senderUserId)"
        use command = new SQLiteCommand(insertSql, connection)
        command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
        command.Parameters.AddWithValue("@uname", uname) |> ignore
        command.Parameters.AddWithValue("@senderUserId", senderUserId) |> ignore
        command.ExecuteNonQuery() |> ignore

    //NEW TWEET FROM THE USER
    let NewTweet (data: TweetData) : ApiResult<Id> = 
        let mutable tweetId = Guid.NewGuid().ToString()
        let insertSql = 
                    "insert into TweetsInfo( tweetId, msg, userId) " + 
                    "values (@tweetId, @msg, @userId)"
        use command = new SQLiteCommand(insertSql, connection)
        command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
        command.Parameters.AddWithValue("@msg", data.text) |> ignore
        command.Parameters.AddWithValue("@userId", data.userid) |> ignore
        command.ExecuteNonQuery() |> ignore
        let  (hashtagArray, mentionedArray) = parseTweet(data.text)
        for hashtag in hashtagArray do
            WriteHashtag(tweetId,hashtag)
        for userMention in mentionedArray do
            WriteMention(tweetId,userMention,data.userid)

        Ok { id = !lastId}
    
    //SUBSCRIBE TO USER
    let SubscribeUser (data: SubscribeData) : ApiResult<Id> = 
        let userid = getUserId(data.uname)
        let subscribeTo = getUserId(data.subscribeTo)
        let insertSql = 
                        "insert into SubscribeInfo(userId, subscribeTo) " + 
                        "values (@userId, @subscribeTo)"
        use command = new SQLiteCommand(insertSql, connection)
        command.Parameters.AddWithValue("@userId", userid) |> ignore
        command.Parameters.AddWithValue("@subscribeTo", subscribeTo) |> ignore
        command.ExecuteNonQuery() |> ignore
        Ok { id = !lastId}  

    //Get tweet for a Tweet ID
    let GetTweet (tweetId:string) =
        let temp = "\""+tweetId+"\"" 
        let mutable tweetMsg = ""
        let mutable senderName = ""
        let selectSql  = "select msg, userId from TweetsInfo where tweetID = " + temp
        let selectCommand = new SQLiteCommand(selectSql, connection)
        let reader = selectCommand.ExecuteReader()
        
        while reader.Read() do
            tweetMsg <- reader.["msg"].ToString()
            let userid = reader.["userId"].ToString()
            let selectSql1  = "select uname from RegistrationInfo where userId = " + userid
            let selectCommand1 = new SQLiteCommand(selectSql1, connection)
            let reader1 = selectCommand1.ExecuteReader()
            while reader1.Read() do
                senderName <- reader1.["uname"].ToString()
        (tweetMsg,senderName)


    //Get my mentioned Tweets
    let GetMentionTweets (uname: string) : ApiResult<TweetFetch[]> =
        let temp = "\""+uname+"\"" 
        let mutable tweetIdList = [||]
        let mutable tweetList = [||]
        let selectSql = "select tweetId from MentionsInfo where uname = " + temp
        let selectCommand = new SQLiteCommand(selectSql, connection)
        let reader = selectCommand.ExecuteReader()
        while reader.Read() do
                        tweetIdList <- Array.append tweetIdList  [|(reader.["tweetId"].ToString())|]
        
        for tweetId in tweetIdList do   
            let mutable (tweetMsg,senderName) = GetTweet(tweetId)
            let tweet = { text = tweetMsg
                          sender = senderName  }
            tweetList <- Array.append tweetList [|tweet|]
        tweetList |> Ok

    //Get subscribed Tweets
    let GetSubscribedTweets(uname: string) : ApiResult<TweetFetch[]> =
        let userid = getUserId(uname)
        let temp = userid.ToString()
        let mutable subscribedToList = [||]
        let mutable tweetIdList = [||]
        let mutable tweetList = [||]
        let selectSql = "select subscribeTo from SubscribeInfo where userId = " +  temp
        let selectCommand = new SQLiteCommand(selectSql, connection)
        let reader = selectCommand.ExecuteReader()
        while reader.Read() do
            subscribedToList <- Array.append subscribedToList  [|System.Convert.ToInt16(reader.["subscribeTo"])|]
        if not (subscribedToList.Length = 0) then
            for subscriberId in subscribedToList do
                let temp1 = subscriberId.ToString()
                printfn "temp = %A" temp1
                let selectSql1 = "select tweetId from TweetsInfo where userId = " +  temp1
                let selectCommand1 = new SQLiteCommand(selectSql1, connection)
                let reader1 = selectCommand1.ExecuteReader()
                while reader1.Read() do
                    tweetIdList <- Array.append tweetIdList  [|(reader1.["tweetId"].ToString())|]
            if not(tweetIdList.Length = 0) then
                printfn "TweetIDLIST = %A" tweetIdList 
                for tweetId in tweetIdList do   
                    let mutable (tweetMsg,senderName) = GetTweet(tweetId)
                    let tweet = { text = tweetMsg
                                  sender = senderName  }
                    tweetList <- Array.append tweetList [|tweet|]
        tweetList |> Ok 

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
        | NewTweet tweetData ->
            JsonContent (Backend.NewTweet tweetData)
        | SubscribeUser subscribeData ->
            JsonContent (Backend.SubscribeUser subscribeData)
        | GetMentionTweets uname ->
            JsonContent (Backend.GetMentionTweets uname)
        | GetSubscribedTweets uname ->
            JsonContent (Backend.GetSubscribedTweets uname)
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
