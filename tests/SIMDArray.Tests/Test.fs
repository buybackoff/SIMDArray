﻿module Test 

open System.Numerics
open System
open System.Diagnostics
open FsCheck
open NUnit                  
open NUnit.Framework
open Swensen.Unquote


let inline compareNums a b =
    let fa = float a
    let fb = float b
    a.Equals b || float(a - b) < 0.00001 || float(b -  a) < 0.00001 ||
    Double.IsNaN fa && Double.IsInfinity fb || Double.IsNaN fb && Double.IsInfinity fa


let inline areEqual (xs: 'T []) (ys: 'T []) =
    match xs, ys with
    | null, null -> true
    | [||], [||] -> true
    | null, _ | _, null -> false
    | _ when xs.Length <> ys.Length -> false
    | _ ->
        let mutable break' = false
        let mutable i = 0
        let mutable result = true
        while i < xs.Length && not break' do
            if xs.[i] <> ys.[i] then 
                break' <- true
                result <- false
            i <- i + 1
        result

open FsCheck.Gen

let inline lenAbove num = Gen.where (fun a -> (^a:(member Length:int)a) > num)
let inline lenBelow num = Gen.where (fun a -> (^a:(member Length:int)a) < num)
let inline between a b = lenAbove a >> lenBelow b

let arrayArb<'a> =  
    Gen.arrayOf Arb.generate<'a> 
    |> between 1 10000 |> Arb.fromGen


let testCount = 10000
let config = 
    { Config.Quick with 
        MaxTest = testCount
        StartSize = 1
    }

let quickCheck prop = Check.One(config, prop)
let sickCheck fn = Check.One(config, Prop.forAll arrayArb fn)

[<Test>]                  
let ``SIMD.clear = Array.clear`` () =
    quickCheck <|
    fun (xs: int []) ->
        (xs.Length > 0 && xs <> [||]) ==>
        let A xs = Array.SIMD.clear xs 0 xs.Length
        let B xs = Array.Clear(xs, 0, xs.Length)
        (lazy test <@ A xs = B xs @>)   |@ "clear" 


[<Test>]                  
let ``SIMD.create = Array.create`` () =
    quickCheck <|
    fun (len: int) (value: int) ->
        (len >= 0 ) ==>
        let A (len:int) (value:int) = Array.SIMD.create len value
        let B (len:int) (value:int) = Array.create len value        
        (lazy test <@ A len value = B len value @>)   |@ "create len value" 

[<Test>]                  
let ``SIMD.init = Array.init`` () =
    quickCheck <|
    fun (len: int) ->
        (len >= 0 ) ==>
        let A (len:int) = Array.SIMD.init len (fun i -> Vector<int>(5))
        let B (len:int) = Array.init len (fun i -> 5)
        (lazy test <@ A len = B len  @>)   |@ "init len" 


[<Test>]                  
let ``SIMD.map = Array.map`` () =
    quickCheck <|
    fun (xs: int []) ->
        (xs.Length > 0 && xs <> [||]) ==>
        let plusA   xs = xs |> Array.SIMD.map (fun x -> x+x)
        let plusB   xs = xs |> Array.map (fun x -> x+x)
        let multA   xs = xs |> Array.SIMD.map (fun x -> x*x)
        let multB   xs = xs |> Array.map (fun x -> x*x)
        let minusA  xs = xs |> Array.SIMD.map (fun x -> x-x)
        let minusB  xs = xs |> Array.map (fun x -> x-x)
        (lazy test <@ plusA xs = plusB xs @>)   |@ "map x + x" .&.
        (lazy test <@ multA xs = multB xs @>)   |@ "map x * x" .&.
        (lazy test <@ minusA xs = minusB xs @>) |@ "map x - x" 


[<Test>]                  
let ``SIMD.mapInPlace = Array.map`` () =
    quickCheck <|
    fun (xs: int []) ->
        (xs.Length > 0 && xs <> [||]) ==>
        let plusA   xs =  
            let copy = Array.copy xs
            copy |> Array.SIMD.mapInPlace (fun x -> x+x)
            copy
        let plusB   xs = xs |> Array.map (fun x -> x+x)
        let multA   xs = 
            let copy = Array.copy xs
            copy |> Array.SIMD.mapInPlace (fun x -> x*x)
            copy
        let multB   xs = xs |> Array.map (fun x -> x*x)
        let minusA  xs = 
            let copy = Array.copy xs
            copy |> Array.SIMD.mapInPlace (fun x -> x-x)
            copy
        let minusB  xs = xs |> Array.map (fun x -> x-x)
        (lazy test <@ plusA xs = plusB xs @>)   |@ "mapInPlace x + x" .&.
        (lazy test <@ multA xs = multB xs @>)   |@ "mapInPlace x * x" .&.
        (lazy test <@ minusA xs = minusB xs @>) |@ "mapInPlace x - x" 


[<Test>]                  
let ``SIMD.map2 = Array.map2`` () =
    quickCheck <|
    fun (xs: int []) ->
        (xs.Length > 0 && xs <> [||] ) ==>
        let xs2 = xs |> Array.map(fun x -> x+1)
        let plusA   xs xs2 = xs |> Array.SIMD.map2 (fun x y -> x+y) xs2
        let plusB   xs xs2 = xs |> Array.map2 (fun x y -> x+y) xs2
        let multA   xs xs2 = xs |> Array.SIMD.map2 (fun (x:Vector<int>) (y:Vector<int>) -> x*y) xs2
        let multB   xs xs2 = xs |> Array.map2 (fun x y -> x*y) xs2
        let minusA  xs xs2 = xs |> Array.SIMD.map2 (fun x y -> x-y) xs2
        let minusB  xs xs2 = xs |> Array.map2 (fun x y -> x-y) xs2
        (lazy test <@ plusA xs xs2 = plusB xs xs2 @>)   |@ "map2 x + y" .&.
        (lazy test <@ multA xs xs2 = multB xs xs2 @>)   |@ "map2 x * y" .&.
        (lazy test <@ minusA xs xs2 = minusB xs xs2 @>) |@ "map2 x - y" 

[<Test>]                  
let ``SIMD.map3 = Array.map3`` () =
    quickCheck <|
    fun (xs: int []) ->
        (xs.Length > 0 && xs <> [||] ) ==>
        let xs2 = xs |> Array.map(fun x -> x+1)
        let xs3 = xs |> Array.map(fun x -> x-1)
        let plusA   xs xs2 xs3 = xs |> Array.SIMD.map3 (fun x y z -> x+y+z) xs2 xs3
        let plusB   xs xs2 xs3 = xs |> Array.map3 (fun x y z -> x+y+z) xs2 xs3
        let multA   xs xs2 xs3 = xs |> Array.SIMD.map3 (fun (x:Vector<int>) (y:Vector<int>) (z:Vector<int>)-> x*y*z) xs2 xs3
        let multB   xs xs2 xs3 = xs |> Array.map3 (fun x y z-> x*y*z) xs2 xs3
        let minusA  xs xs2 xs3 = xs |> Array.SIMD.map3 (fun x y z-> x-y-z) xs2 xs3
        let minusB  xs xs2 xs3 = xs |> Array.map3 (fun x y z-> x-y-z) xs2 xs3
        (lazy test <@ plusA xs xs2 xs3 = plusB xs xs2 xs3 @>)   |@ "map3 x + y + z" .&.
        (lazy test <@ multA xs xs2 xs3 = multB xs xs2 xs3 @>)   |@ "map3 x * y * z" .&.
        (lazy test <@ minusA xs xs2 xs3 = minusB xs xs2 xs3 @>) |@ "map3 x - y - z" 

[<Test>]                  
let ``SIMD.mapi = Array.mapi`` () =
    quickCheck <|
    fun (xs: int []) ->
        (xs.Length > 0 && xs <> [||]) ==>
        let plusA   xs = xs |> Array.SIMD.mapi (fun i x -> x+x)
        let plusB   xs = xs |> Array.mapi (fun i x -> x+x)
        let multA   xs = xs |> Array.SIMD.mapi (fun i x -> x*x)
        let multB   xs = xs |> Array.mapi (fun i x -> x*x)
        let minusA  xs = xs |> Array.SIMD.mapi (fun i x -> x-x)
        let minusB  xs = xs |> Array.mapi (fun i x -> x-x)
        (lazy test <@ plusA xs = plusB xs @>)   |@ "mapi x + x" .&.
        (lazy test <@ multA xs = multB xs @>)   |@ "mapi x * x" .&.
        (lazy test <@ minusA xs = minusB xs @>) |@ "mapi x - x" 

[<Test>]                  
let ``SIMD.mapi2 = Array.mapi2`` () =
    quickCheck <|
    fun (xs: int []) ->
        (xs.Length > 0 && xs <> [||]) ==>
        let xs2 = xs |> Array.map(fun x -> x+1)
        let plusA   xs xs2 = xs |> Array.SIMD.mapi2 (fun i x y -> x+y) xs2
        let plusB   xs xs2 = xs |> Array.mapi2 (fun i x y -> x+y) xs2
        let multA   xs xs2 = xs |> Array.SIMD.mapi2 (fun i x y -> (x:Vector<int>)*(y:Vector<int>)) xs2
        let multB   xs xs2 = xs |> Array.mapi2 (fun i x y -> x*y) xs2
        let minusA  xs xs2 = xs |> Array.SIMD.mapi2 (fun i x y -> x-y) xs2
        let minusB  xs xs2 = xs |> Array.mapi2 (fun i x y -> x-y) xs2
        (lazy test <@ plusA xs xs2 = plusB xs xs2 @>)   |@ "mapi2 x + y" .&.
        (lazy test <@ multA xs xs2 = multB xs xs2 @>)   |@ "mapi2 x * y" .&.
        (lazy test <@ minusA xs xs2 = minusB xs xs2 @>) |@ "mapi2 x - y" 

  

[<Test>]                  
let ``SIMD.sum = Array.sum`` () =
    quickCheck <|
    fun (array: int []) ->
        (array.Length > 0 && array <> [||]) ==>
        lazy (Array.SIMD.sum array = Array.sum array)

[<Test>]                  
let ``SIMD.reduce = Array.reduce`` () =
    quickCheck <|
    fun (array: int []) ->
        (array.Length > 0 && array <> [||]) ==>
        lazy (Array.SIMD.reduce (+) (+) array = Array.reduce (+) array)

[<Test>]                  
let ``SIMD.fold = Array.fold`` () =
    quickCheck <|
    fun (array: int []) ->
        (array.Length > 0 && array <> [||]) ==>
        lazy (Array.SIMD.fold (+) (+) 0 array = Array.fold (+) 0 array)


[<Test>]                  
let ``SIMD.contains = Array.contains`` () =
    quickCheck <|
    fun (array: int []) (value:int) ->
        (array.Length > 0 && array <> [||]) ==>
        lazy (Array.SIMD.contains value array = Array.contains value array)

[<Test>]                  
let ``SIMD.exists = Array.exists`` () =
    quickCheck <|
    fun (array: int []) (value:int) ->
        (array.Length > 0 && array <> [||]) ==>
        lazy (Array.SIMD.exists (fun x -> Vector.EqualsAny(Vector<int>(value),x)) array = Array.exists (fun x -> x = value) array)


[<Test>]                  
let ``SIMD.average = Array.average`` () =
    quickCheck <|
    fun (array: float []) ->
        (array.Length > 0 && array <> [||]) ==>
        lazy ((compareNums (Array.SIMD.average array) (Array.average array)))


[<Test>]                  
let ``SIMD.max = Array.max`` () =
    quickCheck <|
    fun (array: float []) ->
        (array.Length > 0 && array <> [||]) ==>
        lazy ((compareNums (Array.SIMD.max array) (Array.max array)))

[<Test>]                  
let ``SIMD.maxBy = Array.maxBy`` () =
    quickCheck <|
    fun (array: int []) ->
        (array.Length > 0 && array <> [||]) ==>
        lazy ((compareNums (Array.SIMD.maxBy (fun x -> x+x) array) (Array.maxBy (fun x -> x+x) array)))

[<Test>]                  
let ``SIMD.minBy = Array.minBy`` () =
    quickCheck <|
    fun (array: int []) ->
        (array.Length > 0 && array <> [||]) ==>
        lazy ((compareNums (Array.SIMD.minBy (fun x -> x+x) array) (Array.minBy (fun x -> x+x) array)))


[<Test>]                  
let ``SIMD.min = Array.min`` () =
    quickCheck <|
    fun (array: float []) ->
        (array.Length > 0 && array <> [||]) ==>
        lazy (compareNums (Array.SIMD.min array) (Array.min array))





        
            
