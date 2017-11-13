module GiraffeSample.App

open System
open System.IO
open System.Collections.Generic

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

open Giraffe
open Giraffe.HttpHandlers
open Giraffe.Middleware
open Giraffe.Razor.HttpHandlers
open Giraffe.Razor.Middleware
open Giraffe.HttpContextExtensions

open ToDoTypes
open DataAccess

let handleLunchFilter =
    fun (next: HttpFunc) (ctx : HttpContext) ->
        let filter = ctx.BindQueryString<ToDoFilter>()
        let lunchSpots = LunchAccess.getLunches filter
        json lunchSpots next ctx

let handleAddLunch =
    fun (next: HttpFunc) (ctx : HttpContext) ->
        task {
            let! lunch = ctx.BindJson<ToDo>()
            LunchAccess.addLunch lunch
            return! text (sprintf "Added %s to the toto list." lunch.Description) next ctx
        }

let handleUpdateToDo = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! todo = ctx.BindJson<ToDo>()
            LunchAccess.updateToDo todo
            return! text (sprintf "Task %s has been updated." todo.Description) next ctx
        }

let webApp =
    choose [
        GET >=> route "/todo" >=> handleLunchFilter
        POST >=> route "/todo" >=> handleAddLunch
        PUT >=> route "/todo" >=> handleUpdateToDo
        setStatusCode 404 >=> text "Not Found" ]

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffeErrorHandler errorHandler
    app.UseStaticFiles() |> ignore
    app.UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    let sp  = services.BuildServiceProvider()
    let env = sp.GetService<IHostingEnvironment>()
    let viewsFolderPath = Path.Combine(env.ContentRootPath, "Views")
    services.AddMemoryCache() |> ignore
    services.AddRazorEngine viewsFolderPath |> ignore

let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = l.Equals LogLevel.Error
    builder.AddFilter(filter).AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main argv =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0