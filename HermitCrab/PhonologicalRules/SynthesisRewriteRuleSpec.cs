using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class SynthesisRewriteRuleSpec : RewriteRuleSpec
	{
		public SynthesisRewriteRuleSpec(SpanFactory<ShapeNode> spanFactory, MatcherSettings<ShapeNode> matcherSettings, bool isIterative, Pattern<Word, ShapeNode> lhs,
			IEnumerable<RewriteSubrule> subrules)
			: base(lhs.IsEmpty)
		{
			Pattern.Acceptable = match => CheckTarget(match, lhs);

			if (lhs.IsEmpty)
			{
				Pattern.Children.Add(new Constraint<Word, ShapeNode>(FeatureStruct.New().Symbol(HCFeatureSystem.Segment, HCFeatureSystem.Anchor).Value));
			}
			else
			{
				foreach (Constraint<Word, ShapeNode> constraint in lhs.Children.Cast<Constraint<Word, ShapeNode>>())
				{
					var newConstraint = constraint.DeepClone();
					if (isIterative)
						newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
					Pattern.Children.Add(newConstraint);
				}
			}
			Pattern.Freeze();

			int i = 0;
			foreach (RewriteSubrule subrule in subrules)
			{
				if (lhs.Children.Count == subrule.Rhs.Children.Count)
					SubruleSpecs.Add(new FeatureSynthesisRewriteSubruleSpec(spanFactory, matcherSettings, isIterative, subrule, i));
				else if (lhs.Children.Count > subrule.Rhs.Children.Count)
					SubruleSpecs.Add(new NarrowSynthesisRewriteSubruleSpec(spanFactory, matcherSettings, isIterative, lhs.Children.Count, subrule, i));
				else if (lhs.Children.Count == 0)
					SubruleSpecs.Add(new EpenthesisSynthesisRewriteSubruleSpec(spanFactory, matcherSettings, isIterative, subrule, i));
				i++;
			}
		}

		private static bool CheckTarget(Match<Word, ShapeNode> match, Pattern<Word, ShapeNode> lhs)
		{
			foreach (Tuple<ShapeNode, PatternNode<Word, ShapeNode>> tuple in match.Input.Shape.GetNodes(match.Span).Zip(lhs.Children))
			{
				var constraints = (Constraint<Word, ShapeNode>) tuple.Item2;
				if (tuple.Item1.Annotation.Type() != constraints.Type())
					return false;
			}
			return true;
		}
	}
}
