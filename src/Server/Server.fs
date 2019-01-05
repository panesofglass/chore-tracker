open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection

open Frank.Builder
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Shared


let publicPath = Path.GetFullPath "../Client/public"
let port = 8085us

let getInitCounter () : Task<Counter> = task { return { Value = 42 } }
let hello app =
    resource "api/hello" app {
        get (fun (ctx:HttpContext) ->
            task {
                ctx.Response.StatusCode <- 200
                do! ctx.Response.WriteAsync("Hello, world!")
            })
    }

let api app =
    resource "api/init" app {
        get (fun next ctx ->
            task {
                let! counter = getInitCounter()
                return! Successful.OK counter next ctx
            })
    }

let configureServices (services : IServiceCollection) =
    services.AddGiraffe()
            .AddSingleton<Giraffe.Serialization.Json.IJsonSerializer>(Thoth.Json.Giraffe.ThothSerializer())

let initBuilder =
    WebHost
        .CreateDefaultBuilder()
        .UseWebRoot(publicPath)
        .UseContentRoot(publicPath)
        .UseUrls("http://0.0.0.0:" + port.ToString() + "/")

let hostBuilder =
    webHost initBuilder {
        service configureServices

        plug DefaultFilesExtensions.UseDefaultFiles
        plug StaticFileExtensions.UseStaticFiles

        route hello
        route api
    }

hostBuilder.Build().Run()