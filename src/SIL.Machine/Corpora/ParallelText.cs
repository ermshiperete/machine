﻿using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class ParallelText
	{
		private readonly IComparer<object> _segmentRefComparer;

		public ParallelText(IText sourceText, IText targetText, ITextAlignmentCollection textAlignmentCollection = null,
			IComparer<object> segmentRefComparer = null)
		{
			SourceText = sourceText;
			TargetText = targetText;
			TextAlignmentCollection = textAlignmentCollection;
			_segmentRefComparer = segmentRefComparer ?? Comparer<object>.Default;
		}

		public string Id => SourceText.Id;

		public IText SourceText { get; }

		public IText TargetText { get; }

		public ITextAlignmentCollection TextAlignmentCollection { get; }

		public IEnumerable<ParallelTextSegment> Segments
		{
			get
			{
				IEnumerable<TextAlignment> alignments = TextAlignmentCollection?.Alignments
					?? Enumerable.Empty<TextAlignment>();

				using (IEnumerator<TextSegment> enumerator1 = SourceText.Segments.GetEnumerator())
				using (IEnumerator<TextSegment> enumerator2 = TargetText.Segments.GetEnumerator())
				using (IEnumerator<TextAlignment> enumerator3 = alignments.GetEnumerator())
				{
					bool completed = !enumerator1.MoveNext() || !enumerator2.MoveNext();
					while (!completed)
					{
						int compare1 = _segmentRefComparer.Compare(enumerator1.Current.SegmentRef,
							enumerator2.Current.SegmentRef);
						if (compare1 < 0)
						{
							completed = !enumerator1.MoveNext();
						}
						else if (compare1 > 0)
						{
							completed = !enumerator2.MoveNext();
						}
						else
						{
							int compare2;
							do
							{
								compare2 = enumerator3.MoveNext()
									? _segmentRefComparer.Compare(enumerator1.Current.SegmentRef,
										enumerator3.Current.SegmentRef)
									: 1;
							} while (compare2 < 0);

							yield return new ParallelTextSegment(this, enumerator1.Current.SegmentRef,
								enumerator1.Current.Segment, enumerator2.Current.Segment,
								compare2 == 0 ? enumerator3.Current.AlignedWordPairs : null);
							completed = !enumerator1.MoveNext() || !enumerator2.MoveNext();
						}
					}
				}
			}
		}
	}
}
