#r "nuget: Akka" 
#r "nuget: Akka.FSharp" 
#r "nuget: FSharp.Data, Version=3.0.1"

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

let mutable tweetMap = Map.empty

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
        printfn "Registered as %s" uname


let loginUser() =
    printfn "\nEnter login info"
    uname <- Console.ReadLine()
    let url = "http://localhost:5000/api/login/" + uname
    let a = FSharp.Data.JsonValue.Load url
    let c = a.["text"].ToString()
    userid <- a.["userid"].ToString()
    if c = "\"True\"" then
        printfn "Login successful"
        true
    else
        printfn "User not found"
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
    printfn "You tweeted: %s" tweet


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
        printfn "Successfully subscribed to %s" subscribeTo
    else
        printfn "User to subscribe not found"
    

let getMentionedTweets() =
    let mutable count = 0
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
            printfn "[%d] %s tweeted : %s" count senderName tweetmsg
            printfn"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            tweetMap <- Map.add count tweetmsg tweetMap

let getSubscribedTweets() = 
    let mutable count = 0
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
            printfn "[%d] %s tweeted : %s" count senderName tweetmsg
            printfn"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            tweetMap <- Map.add count tweetmsg tweetMap

let getHashtagTweets() = 
    printfn "\nEnter hashtag you want to search"
    let mutable hashtag = Console.ReadLine()
    let mutable count = 0
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
            printfn "[%d] %s tweeted : %s"  count senderName tweetmsg
            printfn"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
            tweetMap <- Map.add count tweetmsg tweetMap
            


let reTweet() = 
    printfn "\nEnter the number of the tweet you want to retweet"
    let mutable tweetCount = Int32.Parse (Console.ReadLine())
    if tweetMap.ContainsKey tweetCount then
        let tweetMsg = tweetMap.[tweetCount]

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
        printfn "You tweeted: %s" tweetMsg
    else
        printfn "No tweet at the ID mentioned"














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
    match getInput() with
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
        reTweet()
        mainMenu()
    | true, 7 ->
        menu()
    | true, 9 ->
        ()
    | _ ->
        printfn "Invalid choice" 
        mainMenu()

menu()
mainMenu()

System.Console.ReadLine() |> ignore