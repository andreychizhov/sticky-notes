module ToDoTypes

type ToDo() =
    member val Id = 0 with get, set
    member val Description = "" with get, set
    member val IsDone = false with get, set
    
[<CLIMutable>]
type ToDoFilter =
    { IsDone: Option<bool> }