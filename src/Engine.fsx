module TwitterApi.Engine


open System
open System.Collections.Generic
open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Templating
open System.Data.SQLite 

//Map with User Name as key, Id as value
let mutable userNameIpMap = Map.empty
let mutable userIpNameMap = Map.empty
let mutable activeUsersSet = Set.empty 

//Initialize Database
let db_init() =

    // if (System.IO.File.Exists("sample.sqlite")) then
    //         System.IO.File.Delete("sample.sqlite")

    let databaseFilename = "sample.sqlite"
    let connectionString = sprintf "Data Source=%s;Version=3;" databaseFilename
    let connection = new SQLiteConnection(connectionString)
    connection.Open()

    //Table for Registrations
    let structureSql =
        "create table if not exists RegistrationInfo (" +
        "userId int," +
        "uname varchar(30)," +
        "password varchar(30)) " 
    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore

    //Table for Tweets
    let structureSql =
            "create table if not exists TweetsInfo (" +
            "tweetId varchar(50)," +
            "msg varchar(50)," +
            "userId int)" 
    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore

    //Table for Hashtags
    let structureSql =
            "create table if not exists HashtagInfo (" +
            "tweetId varchar(50)," +
            "hashtag varchar(50))" 
    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore

    //Table for Mentions
    let structureSql =
            "create table if not exists MentionsInfo (" +
            "tweetId varchar(50)," +
            "uname varchar(50)," +
            "senderUserId int)" 
    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore

    //Table for Subscribers
    let structureSql =
            "create table if not exists SubscribeInfo (" +
            "userId int," + 
            "subscribeTo int)" 
    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore

    //Table for NewsFeed
    let structureSql =
            "create table if not exists NewsFeed (" +
            "userId int," + 
            "tweetId varchar(50))"
    let structureCommand = new SQLiteCommand(structureSql, connection)
    structureCommand.ExecuteNonQuery() |>ignore


/// The types used by this application.
module Model =


    /// Data about User
    type UserData =
        {
            id: int
            uname : string
            pwd: string
        }

    //Tweet information
    type TweetData =
        {
            text: string
            userid: int
        }

    //Subscribe information
    type SubscribeData =
        {
            uname: string
            subscribeTo: string
        }

    //Returned Tweet Results
    type TweetFetch = 
        {
            text: string
            sender: string 
        }

    /// The type of REST API endpoints.
    /// This defines the set of requests accepted by our API.
    type ApiEndPoint =

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

        | [<EndPoint "GET /hashtag-tweets">]
            GetHashtagTweets of hashtag: string
        
        | [<EndPoint "GET /live-tweets">]
            GetLiveTweets of uname: string

        | [<EndPoint "POST /logout"; Json "userData">]
            LogoutUser of userData: UserData
 

    /// The type of all endpoints for the application.
    type EndPoint =


        /// Accepts requests to /api/...
        | [<EndPoint "/api">] Api of Cors<ApiEndPoint>

    /// Error result value.
    type Error = { error : string }

    /// Alias representing the success or failure of an operation.
    /// The Ok case contains a success value to return as JSON.
    /// The Error case contains an HTTP status and a JSON error to return.
    type ApiResult<'T> = Result<'T, Http.Status * Error>

    /// Default result value
    type Id = { id : int }

open Model

/// This module implements the back-end of the application.
module Backend =

    let databaseFilename = "sample.sqlite"
    let connectionString = sprintf "Data Source=%s;Version=3;" databaseFilename
    let connection = new SQLiteConnection(connectionString)
    connection.Open()

    let private lastId = ref 0

    let personNotFound() : ApiResult<'T> =
        Error (Http.Status.NotFound, { error = "Person not found." })

    let getUserId(uname:string) = 
        let userid = userNameIpMap.[uname]
        userid

    //Function to parse tweets and extract hashtags and mentions
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
            userNameIpMap <- Map.add temp !lastId userNameIpMap
            userIpNameMap <- Map.add !lastId temp userIpNameMap
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
           let userid= getUserId(uname)
           activeUsersSet <- activeUsersSet.Add(userid)
           Ok { text = "True"  
                userid = userNameIpMap.[uname] }
        else   
           Ok { text = "False" 
                userid = 0 }

    //LOGOUT USER
    let LogoutUser (data: UserData) : ApiResult<Id> = 
            let userid = getUserId(data.uname)
            let useridString = userid.ToString()
            activeUsersSet <- activeUsersSet.Remove(userid)
            printfn "--------%A" activeUsersSet
            let deleteSql =  "delete from  NewsFeed where userId = " + useridString 
            use command = new SQLiteCommand(deleteSql, connection)
            command.ExecuteNonQuery() |> ignore
            Ok { id = !lastId }


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
        let userIdString = data.userid.ToString()
        printfn "%A" userIdString
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

        ///INSERT TWEET MENTIONS INTO FEEDS
            let userMentionId = getUserId(userMention)
            if activeUsersSet.Contains userMentionId then
                let insertSql = 
                        "insert into NewsFeed( userId, tweetId) " + 
                        "values (@userId, @tweetId )"
                use command = new SQLiteCommand(insertSql, connection)
                command.Parameters.AddWithValue("@userId", userMentionId) |> ignore
                command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
                command.ExecuteNonQuery() |> ignore
 
        ///INSERT TWEETS FOR SUBSCRIBERS
        
        let mutable subscriberList = [||]
        let selectSql =  "select userId from SubscribeInfo where subscribeTo =  " + userIdString
        let selectCommand = new SQLiteCommand(selectSql, connection)
        let reader = selectCommand.ExecuteReader()
        while reader.Read() do
            subscriberList <- Array.append subscriberList  [|System.Convert.ToInt16(reader.["userId"])|]

        for subscriber in subscriberList do
            let tempuser = (int) subscriber
            if activeUsersSet.Contains tempuser then
                let insertSql = 
                        "insert into NewsFeed( userId, tweetId) " + 
                        "values (@userId, @tweetId )"
                use command = new SQLiteCommand(insertSql, connection)
                command.Parameters.AddWithValue("@userId", subscriber) |> ignore
                command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
                command.ExecuteNonQuery() |> ignore

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

    //GET HASHTAG TWEETS
    let GetHashtagTweets(hashtag:string): ApiResult<TweetFetch[]> =
        let temp = "\""+hashtag+"\"" 
        let mutable tweetIdList = [||]
        let mutable tweetList = [||]
        let selectSql = "select tweetId from HashtagInfo where hashtag =  " + temp
        let selectCommand = new SQLiteCommand(selectSql, connection)
        let reader = selectCommand.ExecuteReader()
        while reader.Read() do
            tweetIdList <- Array.append tweetIdList  [|(reader.["tweetId"].ToString())|]
        if not(tweetIdList.Length = 0) then
                for tweetId in tweetIdList do   
                    let mutable (tweetMsg,senderName) = GetTweet(tweetId)
                    let tweet = { text = tweetMsg
                                  sender = senderName  }
                    tweetList <- Array.append tweetList [|tweet|]
        tweetList |> Ok 

    let GetLiveTweets(uname:string):ApiResult<TweetFetch[]> =
        let userid = getUserId(uname).ToString()
        let useridInt = getUserId(uname)
        let mutable tweetIdList = [||]
        let mutable tweetList = [||]
        if activeUsersSet.Contains(useridInt) then
            let selectSql = "select tweetId from NewsFeed where userId =  " + userid
            let selectCommand = new SQLiteCommand(selectSql, connection)
            let reader = selectCommand.ExecuteReader()
            while reader.Read() do
                tweetIdList <- Array.append tweetIdList  [|(reader.["tweetId"].ToString())|]
            if not(tweetIdList.Length = 0) then
                    for tweetId in tweetIdList do   
                        let mutable (tweetMsg,senderName) = GetTweet(tweetId)
                        let tweet = { text = tweetMsg
                                      sender = senderName  }
                        tweetList <- Array.append tweetList [|tweet|]

            for tweetId in tweetIdList do
                let tweetIdString = "\""+tweetId+"\"" 
                let deleteSql =  "delete from  NewsFeed where userId = " + userid + " and tweetId = " +  tweetIdString
                use command = new SQLiteCommand(deleteSql, connection)
                command.ExecuteNonQuery() |> ignore
        tweetList |> Ok 


    

/// The server side , tying everything together.
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
        | GetHashtagTweets hashtag ->
            JsonContent (Backend.GetHashtagTweets hashtag)
        | GetLiveTweets uname ->
            JsonContent (Backend.GetLiveTweets uname)
        | LogoutUser userData ->
            JsonContent (Backend.LogoutUser userData)




    /// The Sitelet parses requests into EndPoint values
    /// and dispatches them to the content function.
    let Main corsAllowedOrigins : Sitelet<EndPoint> =

        db_init()
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            
            | Api api ->
                Content.Cors api (fun allows ->
                    { allows with
                        Origins = corsAllowedOrigins
                        Headers = ["Content-Type"]
                    }
                ) ApiContent
        )
