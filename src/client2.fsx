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

(*type HttpResponseBody1 =
    | Text of string
    | Binary of byte[]*)

type Json = JsonProvider<"""{
    "id": 1,
    "uname": "XVGGH",
    "pwd": "CVBBH"
}""">
//type RegisterMsg = {code : int; uid : string; password : string}

printfn "Enter username to register"
let mutable uname = Console.ReadLine()
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



printfn "\n Enter login info"
let mutable loginUserName = Console.ReadLine()
let url = "http://localhost:5000/api/login/" + loginUserName
let a = FSharp.Data.JsonValue.Load url
let c = a.["text"].ToString()
let userid = a.["userid"].ToString()
if c = "\"True\"" then
    printfn "Login successful"
else
    printfn "User not found"
// for i in ar do
//     printfn "%A" i
//     printfn"~~~~~~~~~~~~"

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

System.Console.ReadLine() |> ignore