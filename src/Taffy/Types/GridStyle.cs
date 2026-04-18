// Port of taffy/src/style/grid.rs (grid-specific style types)
//
// Defines the CSS Grid style types:
//   MinTrackSizingFunction, MaxTrackSizingFunction, TrackSizingFunction
//   GridPlacement, GridAutoFlow

namespace Taffy
{
    // ── MinTrackSizingFunction ────────────────────────────────────────────────
    //
    // Valid tags: Length, Percent, Auto, MinContent, MaxContent.
    // (Fr and FitContent are only valid for the max function.)

    /// <summary>The minimum sizing function for a grid track.</summary>
    public readonly struct MinTrackSizingFunction : IEquatable<MinTrackSizingFunction>
    {
        internal readonly CompactLength _cl;

        private MinTrackSizingFunction(CompactLength cl)
        {
            _cl = cl;
        }

        // ── Factories ─────────────────────────────────────────────────────────
        public static MinTrackSizingFunction Length(float px) => new MinTrackSizingFunction(CompactLength.Length(px));

        public static MinTrackSizingFunction Percent(float pct) =>
            new MinTrackSizingFunction(CompactLength.Percent(pct));

        public static MinTrackSizingFunction Auto() => new MinTrackSizingFunction(CompactLength.Auto());
        public static MinTrackSizingFunction MinContent() => new MinTrackSizingFunction(CompactLength.MinContent());
        public static MinTrackSizingFunction MaxContent() => new MinTrackSizingFunction(CompactLength.MaxContent());

        public static readonly MinTrackSizingFunction ZERO = Length(0f);
        public static readonly MinTrackSizingFunction AUTO = Auto();
        public static readonly MinTrackSizingFunction MIN_CONTENT = MinContent();
        public static readonly MinTrackSizingFunction MAX_CONTENT = MaxContent();

        // ── Conversions from other length types ───────────────────────────────
        public static MinTrackSizingFunction From(LengthPercentage lp) =>
            new MinTrackSizingFunction(lp.Inner);

        public static MinTrackSizingFunction From(MaxTrackSizingFunction max)
        {
            // Fr and FitContent are not valid for min — collapse to auto.
            if (max._cl.IsFr() || max._cl.IsFitContent())
                return AUTO;
            return new MinTrackSizingFunction(max._cl);
        }

        // ── Queries ───────────────────────────────────────────────────────────
        public bool IsIntrinsic() => _cl.IsIntrinsic();
        public bool IsMinOrMaxContent() => _cl.IsMinOrMaxContent();
        public bool UsesPercentage() => _cl.UsesPercentage();

        /// <summary>Resolves the definite value (if any) for this min function.</summary>
        public float? DefiniteValue(float? parentSize)
        {
            if (_cl.Tag == CompactLength.LENGTH_TAG) return _cl.Value;
            if (_cl.Tag == CompactLength.PERCENT_TAG && parentSize.HasValue) return _cl.Value * parentSize.Value;
            return null;
        }

        /// <summary>Resolves the percentage component of this function, if any.</summary>
        public float? ResolvedPercentageSize(float parentSize)
        {
            if (_cl.Tag == CompactLength.PERCENT_TAG) return _cl.Value * parentSize;
            return null;
        }

        public bool Equals(MinTrackSizingFunction other) => _cl == other._cl;
        public override bool Equals(object? obj) => obj is MinTrackSizingFunction o && Equals(o);
        public override int GetHashCode() => _cl.GetHashCode();
    }

    // ── MaxTrackSizingFunction ────────────────────────────────────────────────
    //
    // Valid tags: Length, Percent, Auto, MinContent, MaxContent,
    //             FitContentPx, FitContentPercent, Fr.

    /// <summary>The maximum sizing function for a grid track.</summary>
    public readonly struct MaxTrackSizingFunction : IEquatable<MaxTrackSizingFunction>
    {
        internal readonly CompactLength _cl;

        private MaxTrackSizingFunction(CompactLength cl)
        {
            _cl = cl;
        }

        // ── Factories ─────────────────────────────────────────────────────────
        public static MaxTrackSizingFunction Length(float px) => new MaxTrackSizingFunction(CompactLength.Length(px));

        public static MaxTrackSizingFunction Percent(float pct) =>
            new MaxTrackSizingFunction(CompactLength.Percent(pct));

        public static MaxTrackSizingFunction Auto() => new MaxTrackSizingFunction(CompactLength.Auto());
        public static MaxTrackSizingFunction MinContent() => new MaxTrackSizingFunction(CompactLength.MinContent());
        public static MaxTrackSizingFunction MaxContent() => new MaxTrackSizingFunction(CompactLength.MaxContent());

        public static MaxTrackSizingFunction FitContentPx(float px) =>
            new MaxTrackSizingFunction(CompactLength.FitContentPx(px));

        public static MaxTrackSizingFunction FitContentPercent(float p) =>
            new MaxTrackSizingFunction(CompactLength.FitContentPercent(p));

        public static MaxTrackSizingFunction Fr(float fr) => new MaxTrackSizingFunction(CompactLength.Fr(fr));

        public static readonly MaxTrackSizingFunction ZERO = Length(0f);
        public static readonly MaxTrackSizingFunction AUTO = Auto();
        public static readonly MaxTrackSizingFunction MIN_CONTENT = MinContent();
        public static readonly MaxTrackSizingFunction MAX_CONTENT = MaxContent();

        // ── Conversions from other length types ───────────────────────────────
        public static MaxTrackSizingFunction From(LengthPercentage lp) =>
            new MaxTrackSizingFunction(lp.Inner);

        // ── Queries ───────────────────────────────────────────────────────────
        public bool IsFr() => _cl.IsFr();
        public bool IsIntrinsic() => _cl.IsIntrinsic();
        public bool IsFitContent() => _cl.IsFitContent();
        public bool IsMaxContentAlike() => _cl.IsMaxContentAlike();
        public bool UsesPercentage() => _cl.UsesPercentage();
        public float FlexFactor() => _cl.IsFr() ? _cl.Value : 0f;

        /// <summary>Resolves to a definite value if the function is fixed (Length or resolvable Percent).</summary>
        public float? DefiniteValue(float? parentSize)
        {
            if (_cl.Tag == CompactLength.LENGTH_TAG) return _cl.Value;
            if (_cl.Tag == CompactLength.PERCENT_TAG && parentSize.HasValue) return _cl.Value * parentSize.Value;
            return null;
        }

        /// <summary>Resolves the percentage component of this function, if any.</summary>
        public float? ResolvedPercentageSize(float parentSize)
        {
            if (_cl.Tag == CompactLength.PERCENT_TAG) return _cl.Value * parentSize;
            if (_cl.Tag == CompactLength.FIT_CONTENT_PCT_TAG) return _cl.Value * parentSize;
            return null;
        }

        /// <summary>
        /// For fit-content tracks: the upper limit imposed by the fit-content argument.
        /// Returns infinity for non-fit-content tracks.
        /// </summary>
        public float FitContentLimit(float? axisAvailableSpace)
        {
            if (_cl.Tag == CompactLength.FIT_CONTENT_PX_TAG) return _cl.Value;
            if (_cl.Tag == CompactLength.FIT_CONTENT_PCT_TAG)
                return axisAvailableSpace.HasValue ? _cl.Value * axisAvailableSpace.Value : float.PositiveInfinity;
            return float.PositiveInfinity;
        }

        public bool Equals(MaxTrackSizingFunction other) => _cl == other._cl;
        public override bool Equals(object? obj) => obj is MaxTrackSizingFunction o && Equals(o);
        public override int GetHashCode() => _cl.GetHashCode();
    }

    // ── TrackSizingFunction ───────────────────────────────────────────────────
    //
    // A min/max pair — CSS minmax() or a single value (same for both).

    /// <summary>
    /// Defines the size of a grid track: a min/max pair.
    /// Use the static factories (<see cref="Auto"/>, <see cref="Fr"/>, <see cref="Px"/>, etc.)
    /// or the <see cref="MinMax"/> constructor.
    /// </summary>
    public readonly struct TrackSizingFunction
    {
        public readonly MinTrackSizingFunction Min;
        public readonly MaxTrackSizingFunction Max;

        public TrackSizingFunction(MinTrackSizingFunction min, MaxTrackSizingFunction max)
        {
            Min = min;
            Max = max;
        }

        // ── Common factories ──────────────────────────────────────────────────

        public static TrackSizingFunction MinMax(MinTrackSizingFunction min, MaxTrackSizingFunction max) =>
            new TrackSizingFunction(min, max);

        public static TrackSizingFunction Auto() =>
            new TrackSizingFunction(MinTrackSizingFunction.AUTO, MaxTrackSizingFunction.AUTO);

        public static TrackSizingFunction MinContent() =>
            new TrackSizingFunction(MinTrackSizingFunction.MIN_CONTENT, MaxTrackSizingFunction.MIN_CONTENT);

        public static TrackSizingFunction MaxContent() =>
            new TrackSizingFunction(MinTrackSizingFunction.MAX_CONTENT, MaxTrackSizingFunction.MAX_CONTENT);

        public static TrackSizingFunction Px(float px) =>
            new TrackSizingFunction(MinTrackSizingFunction.Length(px), MaxTrackSizingFunction.Length(px));

        public static TrackSizingFunction Percent(float pct) =>
            new TrackSizingFunction(MinTrackSizingFunction.Percent(pct), MaxTrackSizingFunction.Percent(pct));

        public static TrackSizingFunction Fr(float fr) =>
            new TrackSizingFunction(MinTrackSizingFunction.AUTO, MaxTrackSizingFunction.Fr(fr));

        public static TrackSizingFunction FitContentPx(float px) =>
            new TrackSizingFunction(MinTrackSizingFunction.AUTO, MaxTrackSizingFunction.FitContentPx(px));

        public static TrackSizingFunction FitContentPercent(float pct) =>
            new TrackSizingFunction(MinTrackSizingFunction.AUTO, MaxTrackSizingFunction.FitContentPercent(pct));
    }

    // ── GridLine / OriginZeroLine ─────────────────────────────────────────────
    //
    // Port of taffy/src/compute/grid/types/coordinates.rs

    /// <summary>
    /// A CSS Grid line index in CSS coordinates (1 = first explicit line, -1 = last, 0 invalid).
    /// </summary>
    public readonly struct GridLine : IEquatable<GridLine>
    {
        public readonly short Value;

        public GridLine(short value)
        {
            Value = value;
        }

        public static implicit operator GridLine(short v) => new GridLine(v);

        /// <summary>Converts to OriginZero coordinates.</summary>
        public OriginZeroLine IntoOriginZeroLine(ushort explicitTrackCount)
        {
            int explicitLineCount = explicitTrackCount + 1;
            short oz;
            if (Value > 0) oz = (short)(Value - 1);
            else if (Value < 0) oz = (short)(Value + explicitLineCount);
            else throw new InvalidOperationException("Grid line of zero is invalid");
            return new OriginZeroLine(oz);
        }

        public bool Equals(GridLine other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is GridLine o && Equals(o);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// A grid line index in OriginZero coordinates (0 = left/top edge of explicit grid).
    /// </summary>
    public readonly struct OriginZeroLine
        : IEquatable<OriginZeroLine>, IComparable<OriginZeroLine>
    {
        public readonly short Value;

        public OriginZeroLine(short value)
        {
            Value = value;
        }

        public static OriginZeroLine operator +(OriginZeroLine a, OriginZeroLine b) =>
            new OriginZeroLine((short)(a.Value + b.Value));

        public static OriginZeroLine operator -(OriginZeroLine a, OriginZeroLine b) =>
            new OriginZeroLine((short)(a.Value - b.Value));

        public static OriginZeroLine operator +(OriginZeroLine a, ushort b) => new OriginZeroLine((short)(a.Value + b));
        public static OriginZeroLine operator -(OriginZeroLine a, ushort b) => new OriginZeroLine((short)(a.Value - b));

        /// <summary>Converts to a 0-based index into the GridTrack list (interleaved lines+tracks).</summary>
        public int IntoTrackVecIndex(TrackCounts counts) =>
            TryIntoTrackVecIndex(counts)
            ?? throw new InvalidOperationException(
                $"OriginZeroLine {Value} is out of range for TrackCounts {counts}");

        /// <summary>Returns null if the line is outside the implicit grid (used for absolute items).</summary>
        public int? TryIntoTrackVecIndex(TrackCounts counts)
        {
            if (Value < -(short)counts.NegativeImplicit) return null;
            if (Value > (short)(counts.Explicit + counts.PositiveImplicit)) return null;
            return 2 * ((Value + counts.NegativeImplicit));
        }

        public ushort ImpliedNegativeImplicitTracks() =>
            Value < 0 ? (ushort)(-Value) : (ushort)0;

        public ushort ImpliedPositiveImplicitTracks(ushort explicitTrackCount) =>
            Value > explicitTrackCount ? (ushort)(Value - explicitTrackCount) : (ushort)0;

        public int CompareTo(OriginZeroLine other) => Value.CompareTo(other.Value);
        public bool Equals(OriginZeroLine other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is OriginZeroLine o && Equals(o);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator <(OriginZeroLine a, OriginZeroLine b) => a.Value < b.Value;
        public static bool operator >(OriginZeroLine a, OriginZeroLine b) => a.Value > b.Value;
        public static bool operator <=(OriginZeroLine a, OriginZeroLine b) => a.Value <= b.Value;
        public static bool operator >=(OriginZeroLine a, OriginZeroLine b) => a.Value >= b.Value;
        public override string ToString() => Value.ToString();
    }

    // ── TrackCounts ───────────────────────────────────────────────────────────

    /// <summary>Number of tracks in each section of a grid axis (implicit-before, explicit, implicit-after).</summary>
    public struct TrackCounts : IEquatable<TrackCounts>
    {
        public ushort NegativeImplicit;
        public ushort Explicit;
        public ushort PositiveImplicit;

        public TrackCounts(ushort negativeImplicit, ushort @explicit, ushort positiveImplicit)
        {
            NegativeImplicit = negativeImplicit;
            Explicit = @explicit;
            PositiveImplicit = positiveImplicit;
        }

        public int Len() => NegativeImplicit + Explicit + PositiveImplicit;

        public OriginZeroLine ImplicitStartLine() => new OriginZeroLine((short)-(NegativeImplicit));
        public OriginZeroLine ImplicitEndLine() => new OriginZeroLine((short)(Explicit + PositiveImplicit));

        // ── CellOccupancyMatrix track-index ↔ OriginZero conversions ──────────

        public int OzLineToNextTrack(OriginZeroLine line) => line.Value + NegativeImplicit;

        public (int start, int end) OzLineRangeToTrackRange(Line<OriginZeroLine> r) =>
            (OzLineToNextTrack(r.Start), OzLineToNextTrack(r.End));

        public OriginZeroLine TrackToPrevOzLine(int trackIndex) =>
            new OriginZeroLine((short)(trackIndex - NegativeImplicit));

        public bool Equals(TrackCounts other) =>
            NegativeImplicit == other.NegativeImplicit &&
            Explicit == other.Explicit &&
            PositiveImplicit == other.PositiveImplicit;

        public override bool Equals(object? obj) => obj is TrackCounts o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(NegativeImplicit, Explicit, PositiveImplicit);

        public override string ToString() =>
            $"TrackCounts(-{NegativeImplicit} explicit={Explicit} +{PositiveImplicit})";
    }

    // ── GridPlacement ─────────────────────────────────────────────────────────
    //
    // Port of GenericGridPlacement<GridLine> (the public-facing type).

    /// <summary>How a grid item is placed on a grid axis.</summary>
    public readonly struct GridPlacement : IEquatable<GridPlacement>
    {
        private enum Kind : byte
        {
            Auto,
            Line,
            Span
        }

        private readonly Kind _kind;
        private readonly short _value; // GridLine.Value for Line; span count for Span

        private GridPlacement(Kind kind, short value = 0)
        {
            _kind = kind;
            _value = value;
        }

        // ── Factories ─────────────────────────────────────────────────────────
        public static readonly GridPlacement Auto = new GridPlacement(Kind.Auto);

        public static GridPlacement Line(short lineIndex) => new GridPlacement(Kind.Line, lineIndex);
        public static GridPlacement Line(int lineIndex) => new GridPlacement(Kind.Line, (short)lineIndex);

        public static GridPlacement Span(ushort span) => new GridPlacement(Kind.Span, (short)span);
        public static GridPlacement Span(int span) => new GridPlacement(Kind.Span, (short)span);

        // ── Queries ───────────────────────────────────────────────────────────
        public bool IsAuto => _kind == Kind.Auto;
        public bool IsLine => _kind == Kind.Line;
        public bool IsSpan => _kind == Kind.Span;

        public GridLine AsLine() => new GridLine((short)_value);
        public ushort AsSpan() => (ushort)_value;

        /// <summary>Converts to OriginZero placement, ignoring named lines (unsupported).</summary>
        public OriginZeroGridPlacement IntoOriginZeroIgnoringNamed(ushort explicitTrackCount)
        {
            return _kind switch
            {
                Kind.Auto => OriginZeroGridPlacement.Auto,
                Kind.Span => OriginZeroGridPlacement.Span(AsSpan()),
                Kind.Line when _value == 0 => OriginZeroGridPlacement.Auto,
                Kind.Line => OriginZeroGridPlacement.Line(AsLine().IntoOriginZeroLine(explicitTrackCount)),
                _ => OriginZeroGridPlacement.Auto,
            };
        }

        public bool Equals(GridPlacement other) => _kind == other._kind && _value == other._value;
        public override bool Equals(object? obj) => obj is GridPlacement o && Equals(o);
        public override int GetHashCode() => HashCode.Combine((byte)_kind, _value);

        public override string ToString() => _kind switch
        {
            Kind.Auto => "auto",
            Kind.Line => $"line({_value})",
            Kind.Span => $"span({_value})",
            _ => "auto",
        };
    }

    // ── OriginZeroGridPlacement ───────────────────────────────────────────────

    /// <summary>GridPlacement in OriginZero coordinates (used internally during placement).</summary>
    public readonly struct OriginZeroGridPlacement : IEquatable<OriginZeroGridPlacement>
    {
        private enum Kind : byte
        {
            Auto,
            Line,
            Span
        }

        private readonly Kind _kind;
        private readonly short _value;

        private OriginZeroGridPlacement(Kind kind, short value = 0)
        {
            _kind = kind;
            _value = value;
        }

        public static readonly OriginZeroGridPlacement Auto = new OriginZeroGridPlacement(Kind.Auto);

        public static OriginZeroGridPlacement Line(OriginZeroLine line) =>
            new OriginZeroGridPlacement(Kind.Line, line.Value);

        public static OriginZeroGridPlacement Span(ushort span) => new OriginZeroGridPlacement(Kind.Span, (short)span);

        public bool IsAuto => _kind == Kind.Auto;
        public bool IsLine => _kind == Kind.Line;
        public bool IsSpan => _kind == Kind.Span;

        public OriginZeroLine AsLine() => new OriginZeroLine(_value);
        public ushort AsSpan() => (ushort)_value;

        public bool Equals(OriginZeroGridPlacement other) => _kind == other._kind && _value == other._value;
        public override bool Equals(object? obj) => obj is OriginZeroGridPlacement o && Equals(o);
        public override int GetHashCode() => HashCode.Combine((byte)_kind, _value);
    }

    // ── GridAutoFlow ──────────────────────────────────────────────────────────

    /// <summary>Controls how auto-placed items fill the grid.</summary>
    public enum GridAutoFlow : byte
    {
        /// <summary>Fill rows first (default).</summary>
        Row = 0,

        /// <summary>Fill columns first.</summary>
        Column = 1,

        /// <summary>Fill rows first, dense packing.</summary>
        RowDense = 2,

        /// <summary>Fill columns first, dense packing.</summary>
        ColumnDense = 3,
    }

    // ── Extension helpers ─────────────────────────────────────────────────────

    public static class GridAutoFlowExt
    {
        public static bool IsRow(this GridAutoFlow f) => f == GridAutoFlow.Row || f == GridAutoFlow.RowDense;
        public static bool IsDense(this GridAutoFlow f) => f == GridAutoFlow.RowDense || f == GridAutoFlow.ColumnDense;

        public static AbsoluteAxis PrimaryAxis(this GridAutoFlow f) =>
            f.IsRow() ? AbsoluteAxis.Horizontal : AbsoluteAxis.Vertical;

        public static AbsoluteAxis SecondaryAxis(this GridAutoFlow f) =>
            f.IsRow() ? AbsoluteAxis.Vertical : AbsoluteAxis.Horizontal;
    }

    public static class LineGridPlacementExt
    {
        /// <summary>Convert both ends of a Line&lt;GridPlacement&gt; to OriginZero coordinates.</summary>
        public static Line<OriginZeroGridPlacement> IntoOriginZeroIgnoringNamed(
            this Line<GridPlacement> self, ushort explicitTrackCount) =>
            new Line<OriginZeroGridPlacement>(
                self.Start.IntoOriginZeroIgnoringNamed(explicitTrackCount),
                self.End.IntoOriginZeroIgnoringNamed(explicitTrackCount));

        /// <summary>Returns true if either end is a definite line number (not Auto, not Span).</summary>
        public static bool IsDefinite(this Line<GridPlacement> self) =>
            self.Start.IsLine || self.End.IsLine;
    }

    public static class LineOriginZeroExt
    {
        /// <summary>Returns the number of tracks spanned by this line range.</summary>
        public static ushort Span(this Line<OriginZeroLine> self) =>
            (ushort)Math.Max(self.End.Value - self.Start.Value, 0);

        /// <summary>Returns true if either end is a definite line number.</summary>
        public static bool IsDefinite(this Line<OriginZeroGridPlacement> self) =>
            self.Start.IsLine || self.End.IsLine;

        /// <summary>
        /// Returns the span count for an indefinitely-placed item (both ends are Auto or Span).
        /// If Start is Span(n) → n; if End is Span(n) → n; otherwise 1.
        /// </summary>
        public static ushort IndefiniteSpan(this Line<OriginZeroGridPlacement> self)
        {
            if (self.Start.IsSpan) return self.Start.AsSpan();
            if (self.End.IsSpan) return self.End.AsSpan();
            return 1;
        }

        /// <summary>
        /// Resolves a definite-axis placement to a concrete Line&lt;OriginZeroLine&gt;.
        /// Applies CSS Grid conflict rules: swaps if start ≥ end; expands single auto/span end to span 1.
        /// </summary>
        public static Line<OriginZeroLine> ResolveDefiniteGridLines(this Line<OriginZeroGridPlacement> self)
        {
            if (self.Start.IsLine && self.End.IsLine)
            {
                var s = self.Start.AsLine();
                var e = self.End.AsLine();
                // Rule A+B: if start >= end, treat as span 1 from start
                if (s.Value >= e.Value)
                    return new Line<OriginZeroLine>(s, new OriginZeroLine((short)(s.Value + 1)));
                return new Line<OriginZeroLine>(s, e);
            }

            if (self.Start.IsLine)
            {
                var s = self.Start.AsLine();
                var e = self.End.IsSpan
                    ? s + self.End.AsSpan()
                    : new OriginZeroLine((short)(s.Value + 1));
                return new Line<OriginZeroLine>(s, e);
            }

            if (self.End.IsLine)
            {
                var e = self.End.AsLine();
                var s = self.Start.IsSpan
                    ? e - self.Start.AsSpan()
                    : new OriginZeroLine((short)(e.Value - 1));
                return new Line<OriginZeroLine>(s, e);
            }

            throw new InvalidOperationException(
                "ResolveDefiniteGridLines called on non-definite placement");
        }

        /// <summary>Resolves a placement span in OriginZero coordinates to a concrete start/end line pair.</summary>
        public static Line<OriginZeroLine> ResolveAbsolutePlacement(
            this Line<OriginZeroGridPlacement> self,
            TrackCounts counts)
        {
            // Both lines defined — trivial
            if (self.Start.IsLine && self.End.IsLine)
                return new Line<OriginZeroLine>(self.Start.AsLine(), self.End.AsLine());

            // Start line + span or auto
            if (self.Start.IsLine)
            {
                var start = self.Start.AsLine();
                var end = self.End.IsSpan
                    ? start + self.End.AsSpan()
                    : start + (ushort)1;
                return new Line<OriginZeroLine>(start, end);
            }

            // End line + span or auto
            if (self.End.IsLine)
            {
                var end = self.End.AsLine();
                var start = self.Start.IsSpan
                    ? end - self.Start.AsSpan()
                    : end - (ushort)1;
                return new Line<OriginZeroLine>(start, end);
            }

            // Neither line is definite — use implicit placement (placeholder, handled by placement algorithm)
            return new Line<OriginZeroLine>(counts.ImplicitStartLine(), counts.ImplicitStartLine() + (ushort)1);
        }
    }
}