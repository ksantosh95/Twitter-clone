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
    let json = """{
        "id": 0,
        "uname": " """ + uname + """ ",
        "pwd": "password1"
    }"""
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
    //printfn "%A" response
    //let response1 = string response.Body
    //let text = BitConverter.ToString response.Body
    //printfn "%s" text
    response1

let response = sendCmd "test1"
//printfn "%s" response


// at runtime, load up new sample matching that schema
//let response = Http.Request("http://localhost:5000/api/people/2")
//let samples = Json.Parse(response.Body.ToString())

printfn "Registered as %s" uname

// let url = "http://localhost:5000/api/user"
// let a = FSharp.Data.JsonValue.Load url

// let ar = a.AsArray()
// for i in ar do
//     printfn "%A" i
//     printfn"~~~~~~~~~~~~"