module AsyncUtil

type AsyncResult<'T1, 'T2> = Async<Result<'T1, 'T2>>

module AsyncResult =
    let map f a = async {
        let! result = a
        return result |> Result.map f }
    let bind f a = async {
        match! a with
        | Ok r -> return! f r
        | Error r -> return Error r }
    let liftMap f r = async {
        match r with
        | Ok cch ->  let! r = f cch in return Ok r
        | Error x -> return Error x
    }

module Async =
    let map f a = async {
        let! r = a
        return f r
    }