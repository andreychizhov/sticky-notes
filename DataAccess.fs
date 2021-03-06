namespace DataAccess

open System.IO
open NPoco
open Microsoft.Data.Sqlite

open ToDoTypes

module DataAccess =
    let private connString = "Filename=" + Path.Combine(Directory.GetCurrentDirectory(), "StickyNotes.db")

    let addNote (note: ToDo) =
        use conn = new SqliteConnection(connString)
        conn.Open()
        
        use txn: SqliteTransaction = conn.BeginTransaction()

        let cmd = conn.CreateCommand()
        cmd.Transaction <- txn
        cmd.CommandText <- @"
insert into ToDos (Description, IsDone)
values ($Description, $IsDone)"

        cmd.Parameters.AddWithValue("$Description", note.Description) |> ignore
        cmd.Parameters.AddWithValue("$IsDone", note.IsDone) |> ignore

        cmd.ExecuteNonQuery() |> ignore

        txn.Commit()
        
    let updateToDo (todoItem: ToDo) =
        use conn = new SqliteConnection(connString)
        conn.Open()
        use txn: SqliteTransaction = conn.BeginTransaction()
        let cmd = conn.CreateCommand()
        
        cmd.Transaction <- txn
        
        cmd.CommandText <- @"update todos set Description=$Description, IsDone=$IsDone where Id = $Id"
        
        cmd.Parameters.AddWithValue("$Id", todoItem.Id) |> ignore
        cmd.Parameters.AddWithValue("$Description", todoItem.Description) |> ignore
        cmd.Parameters.AddWithValue("$IsDone", todoItem.IsDone) |> ignore
        
        cmd.ExecuteNonQuery() |> ignore
        
        txn.Commit()

    let private getNoteFetchingQuery filter =
    
        let isDonePart, hasIsDonePart =
            match filter.IsDone with
            | Some v -> (sprintf "IsDone = \"%d\"" (if v then 1 else 0), true) // Sqlite uses ints 0 and 1 for bools.
            | None -> ("", false)

        let hasWhereClause = hasIsDonePart

        let query = 
            "select * from ToDos" + 
            (if hasWhereClause then " where " else "") +
            (if hasIsDonePart then isDonePart else "")

        query

    let getNotes (filter: ToDoFilter) =
        let query = getNoteFetchingQuery filter

        use conn = new SqliteConnection(connString)
        conn.Open()

        use db = new Database(conn)
        db.Fetch<ToDo>(query)