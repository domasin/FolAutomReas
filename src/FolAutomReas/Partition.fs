// ========================================================================= //
// Copyright (c) 2003-2007, John Harrison.                                   //
// Copyright (c) 2012 Eric Taucher, Jack Pappas, Anh-Dung Phan               //
// Copyright (c) 2023 Domenico Masini (derived from lib.fs)
// (See "LICENSE.txt" for details.)                                          //
// ========================================================================= //

namespace FolAutomReas.Lib

/// Union-find algorithm.
[<AutoOpen>]
module Partition = 

    open FolAutomReas.Lib.Fpf

    type pnode<'a> =
        | Nonterminal of 'a 
        | Terminal of 'a * int

    type partition<'a> = 
        | Partition of func<'a, pnode<'a>>

    let rec terminus (Partition f as ptn) a =
        match apply f a with
        | Terminal (p, q) ->
            p, q
        | Nonterminal b ->
            terminus ptn b

    let tryterminus ptn a =
        try terminus ptn a
        with _ -> (a, 1)

    let canonize ptn a =
        fst <| tryterminus ptn a

    let equivalent eqv a b =
        canonize eqv a = canonize eqv b

    let equate (a, b) (Partition f as ptn) =
        let a', na = tryterminus ptn a
        let b', nb = tryterminus ptn b
        if a' = b' then f
        elif na <= nb then
            List.foldBack id [a' |-> Nonterminal b'; b' |-> Terminal (b', na + nb)]     f
        else
            List.foldBack id [b' |-> Nonterminal a'; a' |-> Terminal (a', na + nb)]     f
        |> Partition

    let unequal = Partition undefined

    let equated (Partition f) = dom f
