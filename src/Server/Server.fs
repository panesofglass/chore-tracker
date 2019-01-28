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
open OpenAPITypeProvider
open Shared


let publicPath = Path.GetFullPath "../Client/public"
let port = 8085us

type PetStore = OpenAPIV3Provider<"../Client/public/PetStore.yaml">
let petStore = PetStore()
//let pets = petStore.Paths.``/pets`` // Cannot load System.Private.CoreLib.dll

module Views =
    open GiraffeViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "Frank.Giraffe" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial () =
        h1 [] [ encodedText "Frank.Giraffe" ]

    let index (model : Counter) =
        [
            partial()
            p [] [ encodedText (string model.Value) ]
        ] |> layout

let next (ctx:HttpContext) =
    ctx.SetStatusCode(405)
    Task.FromResult(Some ctx)

let getInitCounter () : Task<Counter> = task { return { Value = 42 } }
let api app =
    resource "api/init" app {
        get (fun ctx ->
            task {
                let! counter = getInitCounter()
                return! Successful.OK counter next ctx
            })
    }

let indexHandler ctx =
    task {
        let! model = getInitCounter()
        let view = Views.index model
        return! htmlView view next ctx
    }

let page app =
    resource "init" app {
        get indexHandler
    }

let configureServices (services : IServiceCollection) =
    services.AddGiraffe()
            .AddSingleton<Giraffe.Serialization.Json.IJsonSerializer>(Thoth.Json.Giraffe.ThothSerializer())

let configureWebHost (builder : IWebHostBuilder) =
    builder.UseWebRoot(publicPath)
           .UseContentRoot(publicPath)
           .UseUrls("http://0.0.0.0:" + port.ToString() + "/")

let hostBuilder =
    webHost (WebHost.CreateDefaultBuilder()) {
        configure configureWebHost
        service configureServices

        plug DefaultFilesExtensions.UseDefaultFiles
        plug StaticFileExtensions.UseStaticFiles

        route api
        route page
    }

hostBuilder.Build().Run()