﻿// ========================================================================= //
// Copyright (c) 2003-2007, John Harrison.                                   //
// Copyright (c) 2012 Eric Taucher, Jack Pappas, Anh-Dung Phan               //
// (See "LICENSE.txt" for details.)                                          //
// ========================================================================= //

#load "initialization.fsx"

open FSharpx.Books.AutomatedReasoning.lib
open FSharpx.Books.AutomatedReasoning.formulas
open FSharpx.Books.AutomatedReasoning.fol
open FSharpx.Books.AutomatedReasoning.lcf
open FSharpx.Books.AutomatedReasoning.lcfprop
open FSharpx.Books.AutomatedReasoning.folderived
open FSharpx.Books.AutomatedReasoning.tactics

fsi.AddPrinter sprint_fol_formula;;
fsi.AddPrinter sprint_thm
fsi.AddPrinter sprint_goal

// pg. 514
// ------------------------------------------------------------------------- //
// A simple example.                                                         //
// ------------------------------------------------------------------------- //

let g0 = 
    set_goal (parse @"
        (forall x. x <= x) /\
        (forall x y z. x <= y /\ y <= z ==> x <= z) /\
        (forall x y. f(x) <= y <=> x <= g(y))
        ==> (forall x y. x <= y ==> f(x) <= f(y)) /\
            (forall x y. x <= y ==> g(x) <= g(y))");;
let g1 = imp_intro_tac "ant" g0;;
let g2 = conj_intro_tac g1;;
let g3 = funpow 2 (auto_tac by ["ant"]) g2;;

extract_thm g3;;
    
// pg. 514
// ------------------------------------------------------------------------- //
// All packaged up together.                                                 //
// ------------------------------------------------------------------------- //

prove (parse @"
    (forall x. x <= x) /\
    (forall x y z. x <= y /\ y <= z ==> x <= z) /\
    (forall x y. f(x) <= y <=> x <= g(y))
    ==> (forall x y. x <= y ==> f(x) <= f(y)) /\
        (forall x y. x <= y ==> g(x) <= g(y))")
        [imp_intro_tac "ant";
        conj_intro_tac;
        auto_tac by ["ant"];
        auto_tac by ["ant"]];;
      
// pg. 518
// ------------------------------------------------------------------------- //
// A simple example.                                                         //
// ------------------------------------------------------------------------- //

let ewd954 = 
    prove (parse @"
        (forall x y. x <= y <=> x * y = x) /\
        (forall x y. f(x * y) = f(x) * f(y))
        ==> forall x y. x <= y ==> f(x) <= f(y)")
        [note("eq_sym",(parse @"forall x y. x = y ==> y = x"))
            using [eq_sym (parset @"x") (parset @"y")];
        note("eq_trans",(parse @"forall x y z. x = y /\ y = z ==> x = z"))
            using [eq_trans (parset @"x") (parset @"y") (parset @"z")];
        note("eq_cong",(parse @"forall x y. x = y ==> f(x) = f(y)"))
            using [axiom_funcong "f" [(parset @"x")] [(parset @"y")]];
        assume ["le",(parse @"forall x y. x <= y <=> x * y = x");
                "hom",(parse @"forall x y. f(x * y) = f(x) * f(y)")];
        fix "x"; fix "y";
        assume ["xy",(parse @"x <= y")];
        so have (parse @"x * y = x") by ["le"];
        so have (parse @"f(x * y) = f(x)") by ["eq_cong"];
        so have (parse @"f(x) = f(x * y)") by ["eq_sym"];
        so have (parse @"f(x) = f(x) * f(y)") by ["eq_trans"; "hom"];
        so have (parse @"f(x) * f(y) = f(x)") by ["eq_sym"];
        so conclude (parse @"f(x) <= f(y)") by ["le"];
        qed];;

// ------------------------------------------------------------------------- //
// More examples not in the main text.                                       //
// ------------------------------------------------------------------------- //

prove (parse @"
        (exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) 
        ==> exists y. p(f(f(f(f(y)))))")
    [assume ["A",(parse @"exists x. p(x)")];
    assume ["B",(parse @"forall x. p(x) ==> p(f(x))")];
    note ("C",(parse @"forall x. p(x) ==> p(f(f(f(f(x)))))"))
    proof
        [have (parse @"forall x. p(x) ==> p(f(f(x)))") by ["B"];
            so conclude (parse @"forall x. p(x) ==> p(f(f(f(f(x)))))") at once;
            qed];
    consider ("a",(parse @"p(a)")) by ["A"];
    take (parset @"a");
    so conclude (parse @"p(f(f(f(f(a)))))") by ["C"];
    qed];;

// ------------------------------------------------------------------------- //
// Alternative formulation with lemma construct.                             //
// ------------------------------------------------------------------------- //

let lemma (s,p) = function
        | (Goals((asl,w)::gls,jfn) as gl) ->
            Goals((asl,p)::((s,p)::asl,w)::gls,
                function (thp::thw::oths) ->
                            jfn(imp_unduplicate(imp_trans thp (shunt thw)) :: oths)
                       | _ -> failwith "malformed input")
        | _ -> failwith "malformed lemma"

prove (parse @"
    (exists x. p(x)) ==> (forall x. p(x) ==> p(f(x)))
    ==> exists y. p(f(f(f(f(y)))))")
    [assume ["A",(parse @"exists x. p(x)")];
    assume ["B",(parse @"forall x. p(x) ==> p(f(x))")];
    lemma ("C",(parse @"forall x. p(x) ==> p(f(f(f(f(x)))))"));
        have (parse @"forall x. p(x) ==> p(f(f(x)))") by ["B"];
        so conclude (parse @"forall x. p(x) ==> p(f(f(f(f(x)))))") at once;
        qed;
    consider ("a",(parse @"p(a)")) by ["A"];
    take (parset @"a");
    so conclude (parse @"p(f(f(f(f(a)))))") by ["C"];
    qed];;

// ------------------------------------------------------------------------- //
// Running a series of proof steps one by one on goals.                      //
// ------------------------------------------------------------------------- //

let run prf g = List.foldBack id (List.rev prf) g

// ------------------------------------------------------------------------- //
// LCF-style interactivity.                                                  //
// ------------------------------------------------------------------------- //

let current_goal = ref[set_goal False]

let g x =
    current_goal := [set_goal x]
    List.head(!current_goal)

let e t =
    current_goal := (t(List.head(!current_goal))::(!current_goal))
    List.head(!current_goal)

let es t =
    current_goal := (run t (List.head(!current_goal))::(!current_goal))
    List.head(!current_goal)

let b () =
    current_goal := List.tail(!current_goal)
    List.head(!current_goal)

// ------------------------------------------------------------------------- //
// Examples.                                                                 //
// ------------------------------------------------------------------------- //

prove (parse @"
    p(a) ==> (forall x. p(x) ==> p(f(x)))
    ==> exists y. p(y) /\ p(f(y))")
    [our thesis at once;
    qed];;

prove (parse @"
    (exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) 
    ==> exists y. p(f(f(f(f(y)))))")
    [assume ["A",(parse @"exists x. p(x)")];
    assume ["B",(parse @"forall x. p(x) ==> p(f(x))")];
    note ("C",(parse @"forall x. p(x) ==> p(f(f(f(f(x)))))")) proof
    [have (parse @"forall x. p(x) ==> p(f(f(x)))") by ["B"];
        so our thesis at once;
        qed];
    consider ("a",(parse @"p(a)")) by ["A"];
    take (parset @"a");
    so our thesis by ["C"];
    qed];;

prove (parse @"
    forall a. p(a) ==> (forall x. p(x) ==> p(f(x)))
        ==> exists y. p(y) /\ p(f(y))")
    [fix "c";
    assume ["A",(parse @"p(c)")];
    assume ["B",(parse @"forall x. p(x) ==> p(f(x))")];
    take (parset @"c");
    conclude (parse @"p(c)") by ["A"];
    note ("C",(parse @"p(c) ==> p(f(c))")) by ["B"];
    so our thesis by ["C"; "A"];
    qed];;

prove (parse @"
    p(c) ==> (forall x. p(x) ==> p(f(x))) 
        ==> exists y. p(y) /\ p(f(y))")
    [assume ["A",(parse @"p(c)")];
    assume ["B",(parse @"forall x. p(x) ==> p(f(x))")];
    take (parset @"c");
    conclude (parse @"p(c)") by ["A"];
    our thesis by ["A"; "B"];
    qed];;

prove (parse @"
    forall a. p(a) ==> (forall x. p(x) ==> p(f(x)))
        ==> exists y. p(y) /\ p(f(y))")
    [fix "c";
    assume ["A",(parse @"p(c)")];
    assume ["B",(parse @"forall x. p(x) ==> p(f(x))")];
    take (parset @"c");
    conclude (parse @"p(c)") by ["A"];
    note ("C",(parse @"p(c) ==> p(f(c))")) by ["B"];
    our thesis by ["C"; "A"];
    qed];;

prove (parse @"
    forall a. p(a) ==> (forall x. p(x) ==> p(f(x))) 
        ==> exists y. p(y) /\ p(f(y))")
    [fix "c";
    assume ["A",(parse @"p(c)")];
    assume ["B",(parse @"forall x. p(x) ==> p(f(x))")];
    take (parset @"c");
    note ("D",(parse @"p(c)")) by ["A"];
    note ("C",(parse @"p(c) ==> p(f(c))")) by ["B"];
    our thesis by ["C"; "A"; "D"];
    qed];;

prove (parse @"
    (p(a) \/ p(b)) ==> q ==> exists y. p(y)")
    [assume ["A",(parse @"p(a) \/ p(b)")];
    assume ["",(parse @"q")];
    cases (parse @"p(a) \/ p(b)") by ["A"];
        take (parset @"a");
        so our thesis at once;
        qed;
        take (parset @"b");
        so our thesis at once;
        qed];;
        
prove (parse @"
    (p(a) \/ p(b)) /\ (forall x. p(x) ==> p(f(x))) ==> exists y. p(f(y))")
    [assume ["base",(parse @"p(a) \/ p(b)");
            "Step",(parse @"forall x. p(x) ==> p(f(x))")];
    cases (parse @"p(a) \/ p(b)") by ["base"]; 
        so note (@"A", (parse @"p(a)")) at once; // use function app instead of value
        note ("X",(parse @"p(a) ==> p(f(a))")) by ["Step"];
        take (parset @"a");
        our thesis by ["A"; "X"];
        qed;
        take (parset @"b");
        so our thesis by ["Step"];
        qed];;

prove (parse @"
    (exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) ==> exists y. p(f(y))")
    [assume ["A",(parse @"exists x. p(x)")];
    assume ["B",(parse @"forall x. p(x) ==> p(f(x))")];
    consider ("a",(parse @"p(a)")) by ["A"];
    so note ("concl",(parse @"p(f(a))")) by ["B"];
    take (parset @"a");
    our thesis by ["concl"];
    qed];;

prove (parse @"
    (forall x. p(x) ==> q(x)) ==> (forall x. q(x) ==> p(x))
        ==> (p(a) <=> q(a))")
    [assume ["A",(parse @"forall x. p(x) ==> q(x)")];
    assume ["B",(parse @"forall x. q(x) ==> p(x)")];
    note ("von",(parse @"p(a) ==> q(a)")) by ["A"];
    note ("bis",(parse @"q(a) ==> p(a)")) by ["B"];
    our thesis by ["von"; "bis"];
    qed];;

//** Mizar-like
// This is an example of Mizar proof, it will not work with this.

//prove
//    (parse @"(p(a) \/ p(b)) /\ (forall x. p(x) ==> p(f(x))) ==> exists y. p(f(y))")
//    [assume ["A",(parse @"antecedent")];
//    note ("Step",(parse @"forall x. p(x) ==> p(f(x))")) by ["A"];
//    per_cases by ["A"];
//        suppose ("base",(parse @"p(a)"));
//        note ("X",(parse @"p(a) ==> p(f(a))")) by ["Step"];
//        take (parset @"a");
//        our thesis by ["base"; "X"];
//        qed;
//
//        suppose ("base",(parse @"p(b)"));
//        our thesis by ["Step"; "base"];
//        qed;
//    endcase];;
       
// ------------------------------------------------------------------------- //
// Some amusing efficiency tests versus a "direct" spec.                     //
// ------------------------------------------------------------------------- //

test002 10;;
// Real: 00:00:14.016, CPU: 00:00:13.953, GC gen0: 29, gen1: 2, gen2: 0
test002 11;;
// Real: 00:00:29.456, CPU: 00:00:29.390, GC gen0: 59, gen1: 4, gen2: 1
test002 12;;
// Real: 00:01:02.199, CPU: 00:01:02.187, GC gen0: 117, gen1: 6, gen2: 1
test002 13;;
// Real: 00:02:10.840, CPU: 00:02:10.781, GC gen0: 233, gen1: 14, gen2: 1
test002 14;;
// Real: 00:04:40.929, CPU: 00:04:40.187, GC gen0: 467, gen1: 25, gen2: 3
test002 15;;