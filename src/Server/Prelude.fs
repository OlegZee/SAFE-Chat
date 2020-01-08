[<AutoOpen>]
module Prelude

let (><) f a b = f b a
let (^) f a = f a
let inline (|OtherwiseFail|) _ = failwith "no choice"
let inline (|OtherwiseFailErr|) message _ = failwith message
