﻿using NUnit.Framework;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Test
{
	[TestFixture]
	public class FeatureStructureTest
	{
		[Test]
		public void DisjunctiveUnify1()
		{
			FeatureSystem featSys = FeatureSystem.New()
				.SymbolicFeature("rank", rank => rank
					.Symbol("clause"))
				.ComplexFeature("subj", subj => subj
					.SymbolicFeature("case", casef => casef
						.Symbol("nom"))
					.SymbolicFeature("number", number => number
						.Symbol("pl")
						.Symbol("sing"))
					.SymbolicFeature("person", person => person
						.Symbol("2")
						.Symbol("3"))
					.StringFeature("lex"))
				.SymbolicFeature("transitivity", transitivity => transitivity
					.Symbol("trans")
					.Symbol("intrans"))
				.SymbolicFeature("voice", voice => voice
					.Symbol("passive")
					.Symbol("active"))
				.ComplexFeature("goal", goal => goal
					.ExtantFeature("case")
					.ExtantFeature("number")
					.ExtantFeature("person")
					.ExtantFeature("lex"))
				.ComplexFeature("actor", actor => actor
					.ExtantFeature("case")
					.ExtantFeature("number")
					.ExtantFeature("person")
					.ExtantFeature("lex"))
				.StringFeature("varFeat1")
				.StringFeature("varFeat2")
				.StringFeature("varFeat3")
				.StringFeature("varFeat4").Value;

			FeatureStruct grammar = FeatureStruct.New(featSys)
				.Feature("rank").EqualTo("clause")
				.Feature("subj").EqualToFeatureStruct(subj => subj
					.Feature("case").EqualTo("nom"))
				.Feature("varFeat1").EqualToVariable("alpha")
				.Feature("varFeat2").EqualTo("value2")
				.And(and => and
					.With(disj => disj
						.Feature("voice").EqualTo("passive")
						.Feature("transitivity").EqualTo("trans")
						.Feature("goal").ReferringTo("subj"))
					.Or(disj => disj
						.Feature("voice").EqualTo("active")
						.Feature("actor").ReferringTo("subj")
						.Feature("varFeat4").EqualToVariable("gamma")))
				.And(and => and
					.With(disj => disj
						.Feature("transitivity").EqualTo("intrans")
						.Feature("actor").EqualToFeatureStruct(actor => actor
							.Feature("person").EqualTo("3")))
					.Or(disj => disj
						.Feature("transitivity").EqualTo("trans")
						.Feature("goal").EqualToFeatureStruct(goal => goal
							.Feature("person").EqualTo("3"))
						.Feature("varFeat3").EqualToVariable("beta")))
				.And(and => and
					.With(disj => disj
						.Feature("number").EqualTo("sing")
						.Feature("subj").EqualToFeatureStruct(subj1 => subj1
							.Feature("number").EqualTo("sing")))
					.Or(disj => disj
						.Feature("number").EqualTo("pl")
						.Feature("subj").EqualToFeatureStruct(subj1 => subj1
							.Feature("number").EqualTo("pl"))
						.Feature("varFeat3").EqualTo("value3")
						.Feature("varFeat2").Not.EqualToVariable("beta"))).Value;

			FeatureStruct constituent = FeatureStruct.New(featSys)
				.Feature("subj").EqualToFeatureStruct(subj => subj
					.Feature("lex").EqualTo("y'all")
					.Feature("person").EqualTo("2")
					.Feature("number").EqualTo("pl"))
				.Feature("varFeat1").EqualTo("value1")
				.Feature("varFeat4").EqualTo("value4").Value;

			var varBindings = new VariableBindings();
			FeatureStruct output;
			Assert.IsTrue(grammar.Unify(constituent, false, varBindings, out output));
		}

		[Test]
		public void DisjunctiveUnify2()
		{
			FeatureSystem featSys = FeatureSystem.New()
				.SymbolicFeature("feat1", feat1 => feat1
					.Symbol("feat1+")
					.Symbol("feat1-"))
				.SymbolicFeature("feat2", feat2 => feat2
					.Symbol("feat2+")
					.Symbol("feat2-"))
				.SymbolicFeature("feat3", feat3 => feat3
					.Symbol("feat3+")
					.Symbol("feat3-"))
				.SymbolicFeature("feat4", feat4 => feat4
					.Symbol("feat4+")
					.Symbol("feat4-"))
				.SymbolicFeature("feat5", feat5 => feat5
					.Symbol("feat5+")
					.Symbol("feat5-"))
				.SymbolicFeature("feat6", feat6 => feat6
					.Symbol("feat6+")
					.Symbol("feat6-"))
				.SymbolicFeature("feat7", feat7 => feat7
					.Symbol("feat7+")
					.Symbol("feat7-")).Value;

			FeatureStruct fs1 = FeatureStruct.New(featSys)
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+"))
					.Or(disj => disj.Symbol("feat2+"))).Value;

			FeatureStruct fs2 = FeatureStruct.New(featSys)
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat3+"))
					.Or(disj => disj.Symbol("feat4+")))
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat5+"))
					.Or(disj => disj.Symbol("feat6+"))).Value;

			FeatureStruct result;
			Assert.IsTrue(fs1.Unify(fs2, out result));
		}

		[Test]
		public void Unify1()
		{
			FeatureSystem featSys = FeatureSystem.New()
				.ComplexFeature("a", a => a
					.SymbolicFeature("b", b => b
						.Symbol("c"))
					.SymbolicFeature("e", e => e
						.Symbol("f"))
					.SymbolicFeature("h", h => h
						.Symbol("j")))
				.ComplexFeature("d", d => d
					.ExtantFeature("b")
					.ExtantFeature("e")
					.ExtantFeature("h"))
				.ComplexFeature("g", g => g
					.ExtantFeature("b")
					.ExtantFeature("e")
					.ExtantFeature("h")).Value;

			FeatureStruct fs1 = FeatureStruct.New(featSys)
				.Feature("a").EqualToFeatureStruct(a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").EqualToFeatureStruct(d => d
					.Feature("e").EqualTo("f"))
				.Feature("g").ReferringTo("d").Value;

			FeatureStruct fs2 = FeatureStruct.New(featSys)
				.Feature("a").EqualToFeatureStruct(a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").ReferringTo("a")
				.Feature("g").EqualToFeatureStruct(g => g
					.Feature("h").EqualTo("j")).Value;

			FeatureStruct result;
			Assert.IsTrue(fs1.Unify(fs2, out result));
		}

		[Test]
		public void Unify2()
		{
			FeatureSystem featSys = FeatureSystem.New()
				.ComplexFeature("a", a => a
					.SymbolicFeature("b", b => b
						.Symbol("c"))
					.SymbolicFeature("e", e => e
						.Symbol("f"))
					.SymbolicFeature("h", h => h
						.Symbol("j")))
				.ComplexFeature("d", d => d
					.ExtantFeature("b")
					.ExtantFeature("e")
					.ExtantFeature("h"))
				.ComplexFeature("g", g => g
					.ExtantFeature("b")
					.ExtantFeature("e")
					.ExtantFeature("h"))
				.ComplexFeature("i", i => i
					.ExtantFeature("b")).Value;

			FeatureStruct fs1 = FeatureStruct.New(featSys)
				.Feature("a").EqualToFeatureStruct(a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").EqualToFeatureStruct(d => d
					.Feature("e").EqualTo("f"))
				.Feature("g").ReferringTo("d")
				.Feature("i").ReferringTo("a", "b").Value;

			FeatureStruct fs2 = FeatureStruct.New(featSys)
				.Feature("a").EqualToFeatureStruct(a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").EqualToFeatureStruct(g => g
					.Feature("h").EqualTo("j"))
				.Feature("g").ReferringTo("a").Value;

			FeatureStruct result;
			Assert.IsTrue(fs1.Unify(fs2, out result));
		}

		[Test]
		public void UnifyVariables()
		{
			FeatureSystem featSys = FeatureSystem.New()
				.SymbolicFeature("a", a => a
					.Symbol("a+", "+")
					.Symbol("a-", "-"))
				.SymbolicFeature("b", b => b
					.Symbol("b+", "+")
					.Symbol("b-", "-")).Value;

			FeatureStruct fs1 = FeatureStruct.New(featSys)
				.Feature("a").EqualToVariable("var1")
				.Symbol("b-").Value;

			FeatureStruct fs2 = FeatureStruct.New(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			FeatureStruct result;
			Assert.IsFalse(fs1.Unify(fs2, out result));

			fs1 = FeatureStruct.New(featSys)
				.Feature("a").EqualToVariable("var1")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.New(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			Assert.IsTrue(fs1.Unify(fs2, out result));

			fs1 = FeatureStruct.New(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.New(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			Assert.IsTrue(fs1.Unify(fs2, out result));

			fs1 = FeatureStruct.New(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.New(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			var varBindings = new VariableBindings();
			varBindings["var1"] = new SymbolicFeatureValue(featSys.GetSymbol("a-"));
			Assert.IsTrue(fs1.Unify(fs2, varBindings, out result));

			varBindings = new VariableBindings();
			varBindings["var1"] = new SymbolicFeatureValue(featSys.GetSymbol("a+"));
			Assert.IsFalse(fs1.Unify(fs2, varBindings, out result));
		}
	}
}