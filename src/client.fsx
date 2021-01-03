#r "nuget: Akka" 
#r "nuget: Akka.FSharp" 
#r "nuget: FSharp.Data, Version=3.0.1"
#r "nuget: Websocket.Client"

open System
open Akka.Actor
open Akka.Actor
open Akka.Configuration
open Akka.Dispatch.SysMsg
open Akka.FSharp
open System.Threading
open FSharp.Data
open FSharp.Data.HttpRequestHeaders
open FSharp.Data.JsonExtensions
open System.Threading
open System.Threading.Tasks

let mutable tweetMap = Map.empty
let mutable feedMap = Map.empty
let mutable count = 0
let mutable childInitialized = false
//REGISTER USER
type Json = JsonProvider<"""{
        "id": 1,
        "uname": "XVGGH",
        "pwd": "CVBBH"
    }""">

let mutable uname = ""
let mutable userid = ""

let registerUser() = 
    printfn "Enter username to register"
    uname <- Console.ReadLine()
    let url = "http://localhost:5000/api/login/" + uname
    let a = FSharp.Data.JsonValue.Load url
    let c = a.["text"].ToString()
    if c = "\"True\"" then
        printfn "User already exists"
    else
        let sendCmd cmd =

            let json = sprintf """{"id": 0, "uname": "%s" , "pwd" : "password1" }""" uname
            let response = Http.Request(
                                        "http://localhost:5000/api/register-user",
                                        httpMethod = "POST",
                                        headers = [ ContentType HttpContentTypes.Json ],
                                        body = TextRequest json
                )
            let r1 = response.Body
            let response1 =
                match r1 with
                | Text a -> a
                | Binary b -> System.Text.ASCIIEncoding.ASCII.GetString b
          
            response1
        let response = sendCmd "test1"
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        printfn "\t\tRegistered as %s" uname
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"


let loginUser() =
    printfn "\nEnter login info"
    uname <- Console.ReadLine()
    let url = "http://localhost:5000/api/login/" + uname
    let a = FSharp.Data.JsonValue.Load url
    let c = a.["text"].ToString()
    userid <- a.["userid"].ToString()
    if c = "\"True\"" then
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        printfn "\t\tLogin successful"
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        true
    else
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        printfn "\t\tUser not found"
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        false

let newTweet() =
    printfn "\nEnter Tweet"
    let mutable tweet = Console.ReadLine()
    let sendTweet twt =
        let json = sprintf """{  "text" : "%s" , "userid": %s }"""  tweet userid
        let response = Http.Request(
                                    "http://localhost:5000/api/new-tweet",
                                    httpMethod = "POST",
                                    headers = [ ContentType HttpContentTypes.Json ],
                                    body = TextRequest json
            )
        let r1 = response.Body
        let response1 =
            match r1 with
            | Text a -> a
            | Binary b -> System.Text.ASCIIEncoding.ASCII.GetString b
      
        response1
    let NewTweet = sendTweet "test1"
    printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
    printfn "\t\tYou tweeted: %s" tweet
    printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"


let logoutUser() =
    let sendCmd cmd =

            let json = sprintf """{"id": 0, "uname": "%s" , "pwd" : "password1" }""" uname
            let response = Http.Request(
                                        "http://localhost:5000/api/logout",
                                        httpMethod = "POST",
                                        headers = [ ContentType HttpContentTypes.Json ],
                                        body = TextRequest json
                )
            let r1 = response.Body
            let response1 =
                match r1 with
                | Text a -> a
                | Binary b -> System.Text.ASCIIEncoding.ASCII.GetString b
          
            response1
    let response = sendCmd "test1"
    printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
    printfn "\t\tLogged out" 
    printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"




let subscribe() =  
    printfn "\nEnter User you want to subscribe to"
    let mutable subscribeTo = Console.ReadLine()

    let url = "http://localhost:5000/api/login/" + subscribeTo
    let a = FSharp.Data.JsonValue.Load url
    let c = a.["text"].ToString()
    if c = "\"True\"" then
        let subscribe sub =

            let json = sprintf """{  "uname" : "%s" , "subscribeTo": "%s" }"""  uname subscribeTo
            let response = Http.Request(
                                        "http://localhost:5000/api/subscribe",
                                        httpMethod = "POST",
                                        headers = [ ContentType HttpContentTypes.Json ],
                                        body = TextRequest json
                )
            let r1 = response.Body
            let response1 =
                match r1 with
                | Text a -> a
                | Binary b -> System.Text.ASCIIEncoding.ASCII.GetString b
          
            response1

        let NewSubscription = subscribe "test1"
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        printfn "\t\tSuccessfully subscribed to %s" subscribeTo
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
    else
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        printfn "\t\tUser to subscribe not found"
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
    

let getMentionedTweets() =
    count <- 0
    let url = "http://localhost:5000/api/mention/" + uname
    let mentionedTweetJson = FSharp.Data.JsonValue.Load url
    let mentionedTweetArray = mentionedTweetJson.AsArray()
    if mentionedTweetArray.Length = 0 then
        printfn "No mentioned Tweets"
    else
        tweetMap <- Map.empty
        for tweet in mentionedTweetArray do
            count <- count + 1
            let tweetmsg = tweet.["text"].ToString()
            let senderName  = tweet.["sender"].ToString()
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            printfn "\t\t[%d] %s tweeted : %s" count senderName tweetmsg
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            tweetMap <- Map.add count tweetmsg tweetMap

let getSubscribedTweets() = 
    count <- 0
    let url = "http://localhost:5000/api/subscribed-tweets/" + uname
    let SubscribedTweetJson = FSharp.Data.JsonValue.Load url
    let SubscribedTweetArray = SubscribedTweetJson.AsArray()
    if SubscribedTweetArray.Length = 0 then
        printfn "No tweets from Subscribed Accounts"
    else
        tweetMap <- Map.empty
        for tweet in SubscribedTweetArray do
            count <- count + 1
            let tweetmsg = tweet.["text"].ToString()
            let senderName  = tweet.["sender"].ToString()
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            printfn "\t\t[%d] %s tweeted : %s" count senderName tweetmsg
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            tweetMap <- Map.add count tweetmsg tweetMap

let getHashtagTweets() = 
    printfn "\nEnter hashtag you want to search"
    let mutable hashtag = Console.ReadLine()
    count <- 0
    let url = "http://localhost:5000/api/hashtag-tweets/" + hashtag
    let HashtagTweetJson = FSharp.Data.JsonValue.Load url
    let HashtagTweetArray = HashtagTweetJson.AsArray()
    if HashtagTweetArray.Length = 0 then
        printfn "No tweets for this hashtag"
    else
        tweetMap <- Map.empty
        for tweet in HashtagTweetArray do
            count <- count + 1
            let tweetmsg = tweet.["text"].ToString()
            let senderName  = tweet.["sender"].ToString()
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            printfn "\t\t[%d] %s tweeted : %s"  count senderName tweetmsg
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            tweetMap <- Map.add count tweetmsg tweetMap
            


let reTweet(request:string) = 
    printfn "\nEnter the number of the tweet you want to retweet"
    let mutable tweetCount = Int32.Parse (Console.ReadLine())
    let mutable tweetMsg = ""
    if request.Equals "Tweet" then
        if tweetMap.ContainsKey tweetCount then
            tweetMsg <- tweetMap.[tweetCount]
        else
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            printfn "\t\tNo tweet at the ID mentioned"
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
    else
        if feedMap.ContainsKey tweetCount then
            tweetMsg <- feedMap.[tweetCount]
        else
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            printfn "\t\tNo tweet at the ID mentioned"
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
    if not (tweetMsg.Equals "") then
        let sendTweet twt =

            let json = sprintf """{  "text" : %s , "userid": %s }"""  tweetMsg userid
            let response = Http.Request(
                                        "http://localhost:5000/api/new-tweet",
                                        httpMethod = "POST",
                                        headers = [ ContentType HttpContentTypes.Json ],
                                        body = TextRequest json
                )
            let r1 = response.Body
            let response1 =
                match r1 with
                | Text a -> a
                | Binary b -> System.Text.ASCIIEncoding.ASCII.GetString b
          
            response1
        let NewTweet = sendTweet "test1"
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        printfn "\t\tYou tweeted: %s" tweetMsg
        printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"


let getFeed() =
    count <- 0
    let mutable result= false
    let url = "http://localhost:5000/api/live-tweets/" + uname
    let FeedTweetJson = FSharp.Data.JsonValue.Load url
    let FeedTweetArray = FeedTweetJson.AsArray()
    if FeedTweetArray.Length <> 0 then
        tweetMap <- Map.empty
        for tweet in FeedTweetArray do
            count <- count + 1
            let tweetmsg = tweet.["text"].ToString()
            let senderName  = tweet.["sender"].ToString()
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            printfn "\t\t[%d] %s tweeted : %s"  count senderName tweetmsg
            printfn"\t\t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            tweetMap <- Map.add count tweetmsg tweetMap
            result <- true
    result









let mutable exit = false

let printLoginMenu () =
    printfn "\n[----------- LOGIN SCREEN -----------]"
    printfn "1. Register"
    printfn "2. Login"
    printf "Enter your choice: "

let getInput () = Int32.TryParse (Console.ReadLine())

let rec menu () =
    printLoginMenu()
    match getInput() with
    | true, 1 -> 
        registerUser()
        menu()
    | true, 2 -> 
        let validLogin = loginUser()
        if not validLogin then
            menu()

            
    | _ ->
        printfn "Invalid choice"  
        menu()

let printMainMenu () =
    printfn "\n\n[----------- MAIN SCREEN -----------]"
    printfn "1. Tweet\n2. Subscribe\n3. Get Subscribed Tweets\n4. Get My Mentions\n5. Get Hashtag Tweets\n6. ReTweet\n7. Logout\n9. Exit"
    printfn "Enter your choice: "

let getNewInput () = Int32.TryParse (Console.ReadLine())

let rec mainMenu () =
    printMainMenu()
    match getNewInput() with
    | true, 1 -> 
        newTweet()
        mainMenu()
    | true, 2 -> 
        subscribe()
        mainMenu()
    | true, 3 ->
        getSubscribedTweets()
        mainMenu()
    | true, 4 ->
        getMentionedTweets()
        mainMenu()
    | true, 5 ->
        getHashtagTweets()
        mainMenu()
    | true, 6 ->
        reTweet("Tweet")
        mainMenu()
    | true, 7 ->
        logoutUser()

    | true, 9 ->
        exit <- true
        ()
    | _ ->
        printfn "Invalid choice" 
        mainMenu()







let clientActor()(mailbox : Actor<_>) = 
    let ws = new ClientWebSocket()
    let url = sprintf "ws://localhost/8080/api/websocket/%s" uname
    let wsUri = Uri(url)
    let wcts = CancellationToken()
    let connectTask = ws.ConnectAsync(wsUri,wcts)
    let rec loop() =  actor {
      
        
        Thread.Sleep(5000) 
        let validTweet = getFeed()

        return! loop()
        }
    loop()


let beginClientActor() =
    let clientsystem = ActorSystem.Create("client")
    let clientActorConfig = clientActor()
    let childName = "client"
    let clientActorRef = spawn clientsystem childName clientActorConfig
    printf ""

while not exit do
    menu()
    if not childInitialized then
        beginClientActor()
        childInitialized <- true
    mainMenu()

System.Console.ReadLine() |> ignore