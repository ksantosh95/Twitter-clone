#r "nuget: Akka" 
#r "nuget: Akka.FSharp" 
#r "nuget: FSharp.Data, Version=3.0.1"
//#r "nuget: WebSharper" 
//#r "nuget: WebSharper.Suave, Version=4.7.0.266" 
//#r "nuget: WebSharper.FSharp, Version=4.7.0.423" 
//#r "nuget: FsPickler, Version=3.4.0" 
//#r "nuget: FSharp.Core, Version=4.5.1"
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
    let mutable loginUserName = Console.ReadLine()
    let url = "http://localhost:5000/api/login/" + loginUserName
    let a = FSharp.Data.JsonValue.Load url
    let c = a.["text"].ToString()
    userid <- a.["userid"].ToString()
    if c = "\"True\"" then
        printfn "Login successful"
    else
        printfn "User not found"

let newTweet() =
    printfn "\n Enter Tweet"
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
    printf ""


let subscribe() =  
    printfn "\n Enter User you want to subscribe to"
    let mutable subscribeTo = Console.ReadLine()
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
    printf ""

let printLoginMenu () =
    printfn "[ LOGIN SCREEN ]"
    printfn "1. Register"
    printfn "2. Login"
    printf "Enter your choise: "

let getInput () = Int32.TryParse (Console.ReadLine())

let rec menu () =
    printLoginMenu()
    match getInput() with
    | true, 1 -> 
        registerUser()
        menu()
    | true, 2 -> 
        loginUser()
    | _ -> menu()

let printMainMenu () =
    printfn "\n\n\n[ MAIN SCREEN ]"
    printfn "1. Tweet"
    printfn "2. Subscribe"
    printf "Enter your choise: "

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
    | _ -> mainMenu()

menu()
mainMenu()

System.Console.ReadLine() |> ignore