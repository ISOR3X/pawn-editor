// Port of taffy/src/compute/grid/types/
//
// Internal types used by the grid layout algorithm:
//   GridTrack    — per-track sizing state
//   GridItem     — per-item state (placement + sizing caches)
//   CellOccupancyMatrix — 2D placement occupancy grid

namespace Taffy;
// ── GridTrackKind ─────────────────────────────────────────────────────────

internal enum GridTrackKind : byte
{
    Track,
    Gutter
}

// ── GridTrack ─────────────────────────────────────────────────────────────
// Port of taffy/src/compute/grid/types/grid_track.rs

internal sealed class GridTrack
{
    /// <summary>Resolved base size of the track.</summary>
    public float BaseSize;

    public float BaseSizePlannedIncrease;

    // Scratch fields used by the track-sizing algorithm
    public float ContentAlignmentAdjustment;

    /// <summary>Growth limit (may be infinity). Scratch value during track sizing.</summary>
    public float GrowthLimit;

    public float GrowthLimitPlannedIncrease;
    public bool InfinitelyGrowable;
    public bool IsCollapsed;
    public float ItemIncurredIncrease;
    public GridTrackKind Kind;
    public MaxTrackSizingFunction MaxTrackSizingFunction;

    public MinTrackSizingFunction MinTrackSizingFunction;

    /// <summary>Accumulated offset from the start of the grid container (set during alignment).</summary>
    public float Offset;

    // ── Constructors ──────────────────────────────────────────────────────

    public static GridTrack NewTrack(MinTrackSizingFunction min, MaxTrackSizingFunction max)
    {
        return new GridTrack
        {
            Kind = GridTrackKind.Track,
            MinTrackSizingFunction = min,
            MaxTrackSizingFunction = max
        };
    }

    public static GridTrack NewGutter(LengthPercentage size)
    {
        return new GridTrack
        {
            Kind = GridTrackKind.Gutter,
            MinTrackSizingFunction = MinTrackSizingFunction.From(size),
            MaxTrackSizingFunction = MaxTrackSizingFunction.From(size)
        };
    }

    // ── Queries ───────────────────────────────────────────────────────────

    public bool IsFlexible()
    {
        return MaxTrackSizingFunction.IsFr();
    }

    public bool UsesPercentage()
    {
        return MinTrackSizingFunction.UsesPercentage() || MaxTrackSizingFunction.UsesPercentage();
    }

    public bool HasIntrinsicSizingFunction()
    {
        return MinTrackSizingFunction.IsIntrinsic() || MaxTrackSizingFunction.IsIntrinsic();
    }

    public float FlexFactor()
    {
        return MaxTrackSizingFunction.FlexFactor();
    }

    public float FitContentLimit(float? axisAvailableSpace)
    {
        return MaxTrackSizingFunction.FitContentLimit(axisAvailableSpace);
    }

    public float FitContentLimitedGrowthLimit(float? axisAvailableSpace)
    {
        return MathF.Min(GrowthLimit, FitContentLimit(axisAvailableSpace));
    }

    public void Collapse()
    {
        IsCollapsed = true;
        MinTrackSizingFunction = MinTrackSizingFunction.ZERO;
        MaxTrackSizingFunction = MaxTrackSizingFunction.ZERO;
    }

    /// <summary>
    ///     Returns the definite upper limit for track sizing, or null if none.
    ///     For FitContent tracks this uses the fit-content argument as the limit.
    /// </summary>
    public float? DefiniteLimit(float? parentSize)
    {
        var definite = MaxTrackSizingFunction.DefiniteValue(parentSize);
        if (definite.HasValue) return definite;
        if (MaxTrackSizingFunction.IsFitContent())
            return MaxTrackSizingFunction.FitContentLimit(parentSize);
        return null;
    }
}

// ── CellOccupancyState ────────────────────────────────────────────────────

internal enum CellOccupancyState : byte
{
    Unoccupied = 0,
    DefinitelyPlaced,
    AutoPlaced
}

// ── CellOccupancyMatrix ───────────────────────────────────────────────────
// Port of taffy/src/compute/grid/types/cell_occupancy.rs
//
// Flat row-major 2D array. Rows = grid rows, Cols = grid columns.

internal sealed class CellOccupancyMatrix
{
    private TrackCounts _colCounts;
    private CellOccupancyState[] _data = null!;

    private TrackCounts _rowCounts;

    /// <summary>Returns the current number of rows in the matrix.</summary>
    public int Rows { get; private set; }

    /// <summary>Returns the current number of columns in the matrix.</summary>
    public int Cols { get; private set; }

    public static CellOccupancyMatrix WithTrackCounts(TrackCounts cols, TrackCounts rows)
    {
        var m = new CellOccupancyMatrix();
        m._rowCounts = rows;
        m._colCounts = cols;
        m.Rows = rows.Len();
        m.Cols = cols.Len();
        m._data = new CellOccupancyState[m.Rows * m.Cols];
        return m;
    }

    public TrackCounts TrackCounts(AbsoluteAxis axis)
    {
        return axis == AbsoluteAxis.Horizontal ? _colCounts : _rowCounts;
    }

    private CellOccupancyState Get(int row, int col)
    {
        return _data[row * Cols + col];
    }

    private void Set(int row, int col, CellOccupancyState state)
    {
        _data[row * Cols + col] = state;
    }

    // ── Range checks and expansion ────────────────────────────────────────

    public bool IsAreaInRange(AbsoluteAxis primaryAxis,
        (int start, int end) primaryRange,
        (int start, int end) secondaryRange)
    {
        var primaryCount = TrackCounts(primaryAxis).Len();
        var secondaryCount = TrackCounts(primaryAxis.OtherAxis()).Len();
        if (primaryRange.start < 0 || primaryRange.end > primaryCount) return false;
        if (secondaryRange.start < 0 || secondaryRange.end > secondaryCount) return false;
        return true;
    }

    private void ExpandToFitRange((int start, int end) rowRange, (int start, int end) colRange)
    {
        var reqNegRows = Math.Max(-rowRange.start, 0);
        var reqPosRows = Math.Max(rowRange.end - Rows, 0);
        var reqNegCols = Math.Max(-colRange.start, 0);
        var reqPosCols = Math.Max(colRange.end - Cols, 0);

        if (reqNegRows == 0 && reqPosRows == 0 && reqNegCols == 0 && reqPosCols == 0) return;

        var newRows = Rows + reqNegRows + reqPosRows;
        var newCols = Cols + reqNegCols + reqPosCols;
        var newData = new CellOccupancyState[newRows * newCols];

        // Copy existing data into new (shifted) positions
        for (var r = 0; r < Rows; r++)
        for (var c = 0; c < Cols; c++)
            newData[(r + reqNegRows) * newCols + c + reqNegCols] = Get(r, c);

        _data = newData;
        Rows = newRows;
        Cols = newCols;

        _rowCounts.NegativeImplicit += (ushort)reqNegRows;
        _rowCounts.PositiveImplicit += (ushort)reqPosRows;
        _colCounts.NegativeImplicit += (ushort)reqNegCols;
        _colCounts.PositiveImplicit += (ushort)reqPosCols;
    }

    // ── Mark / query ──────────────────────────────────────────────────────

    /// <summary>
    ///     Marks all cells in the specified row/column ranges as occupied with the given state.
    ///     Expands the matrix if needed.
    /// </summary>
    public void MarkAreaAs(AbsoluteAxis primaryAxis,
        Line<OriginZeroLine> primarySpan,
        Line<OriginZeroLine> secondarySpan,
        CellOccupancyState state)
    {
        var (rowSpan, colSpan) = primaryAxis == AbsoluteAxis.Vertical
            ? (primarySpan, secondarySpan)
            : (secondarySpan, primarySpan);

        var rowRange = _rowCounts.OzLineRangeToTrackRange(rowSpan);
        var colRange = _colCounts.OzLineRangeToTrackRange(colSpan);

        ExpandToFitRange(rowRange, colRange);

        for (var r = rowRange.start; r < rowRange.end; r++)
        for (var c = colRange.start; c < colRange.end; c++)
            Set(r, c, state);
    }

    /// <summary>Returns the occupancy state of a cell by CellOccupancyMatrix indices.</summary>
    public CellOccupancyState CellState(int row, int col)
    {
        return Get(row, col);
    }

    /// <summary>Returns true if any cell in the primary-span × secondary-span area is occupied.</summary>
    public bool IsAreaOccupied(AbsoluteAxis primaryAxis,
        (int start, int end) primaryRange,
        (int start, int end) secondaryRange)
    {
        var (rowRange, colRange) = primaryAxis == AbsoluteAxis.Vertical
            ? (primaryRange, secondaryRange)
            : (secondaryRange, primaryRange);

        // Out-of-bounds cells are considered unoccupied (mirrors Rust: None => continue).
        for (var r = rowRange.start; r < rowRange.end; r++)
        for (var c = colRange.start; c < colRange.end; c++)
        {
            if (r < 0 || r >= Rows || c < 0 || c >= Cols) continue;
            if (Get(r, c) != CellOccupancyState.Unoccupied) return true;
        }

        return false;
    }

    /// <summary>Returns true if any cell in the specified column (by index) is occupied.</summary>
    public bool ColumnIsOccupied(int colIndex)
    {
        for (var r = 0; r < Rows; r++)
            if (Get(r, colIndex) != CellOccupancyState.Unoccupied)
                return true;
        return false;
    }

    /// <summary>Returns true if any cell in the specified row (by index) is occupied.</summary>
    public bool RowIsOccupied(int rowIndex)
    {
        for (var c = 0; c < Cols; c++)
            if (Get(rowIndex, c) != CellOccupancyState.Unoccupied)
                return true;
        return false;
    }

    // ── Line-based area query ─────────────────────────────────────────────

    /// <summary>Returns true if all cells in the given OriginZero line area are unoccupied.</summary>
    public bool LineAreaIsUnoccupied(
        AbsoluteAxis primaryAxis,
        Line<OriginZeroLine> primarySpan,
        Line<OriginZeroLine> secondarySpan)
    {
        var primaryRange = TrackCounts(primaryAxis).OzLineRangeToTrackRange(primarySpan);
        var secondaryRange = TrackCounts(primaryAxis.OtherAxis()).OzLineRangeToTrackRange(secondarySpan);
        return !IsAreaOccupied(primaryAxis, primaryRange, secondaryRange);
    }

    // ── First / Last of type ──────────────────────────────────────────────

    /// <summary>
    ///     Searches backwards along the primary axis in the secondary-axis track identified by
    ///     <paramref name="startAt" /> and returns the OriginZeroLine of the last cell with the given state.
    /// </summary>
    public OriginZeroLine? LastOfType(AbsoluteAxis primaryAxis, OriginZeroLine startAt, CellOccupancyState kind)
    {
        var trackCounts = TrackCounts(primaryAxis.OtherAxis());
        var trackIdx = trackCounts.OzLineToNextTrack(startAt);
        int? maybeIdx = null;
        if (primaryAxis == AbsoluteAxis.Horizontal)
        {
            if (trackIdx >= 0 && trackIdx < Rows)
                for (var c = Cols - 1; c >= 0; c--)
                    if (Get(trackIdx, c) == kind)
                    {
                        maybeIdx = c;
                        break;
                    }
        }
        else
        {
            if (trackIdx >= 0 && trackIdx < Cols)
                for (var r = Rows - 1; r >= 0; r--)
                    if (Get(r, trackIdx) == kind)
                    {
                        maybeIdx = r;
                        break;
                    }
        }

        return maybeIdx.HasValue ? trackCounts.TrackToPrevOzLine(maybeIdx.Value) : null;
    }

    /// <summary>
    ///     Searches forwards along the primary axis in the secondary-axis track identified by
    ///     <paramref name="startAt" /> and returns the OriginZeroLine of the first cell with the given state.
    /// </summary>
    public OriginZeroLine? FirstOfType(AbsoluteAxis primaryAxis, OriginZeroLine startAt, CellOccupancyState kind)
    {
        var trackCounts = TrackCounts(primaryAxis.OtherAxis());
        var trackIdx = trackCounts.OzLineToNextTrack(startAt);
        int? maybeIdx = null;
        if (primaryAxis == AbsoluteAxis.Horizontal)
        {
            if (trackIdx >= 0 && trackIdx < Rows)
                for (var c = 0; c < Cols; c++)
                    if (Get(trackIdx, c) == kind)
                    {
                        maybeIdx = c;
                        break;
                    }
        }
        else
        {
            if (trackIdx >= 0 && trackIdx < Cols)
                for (var r = 0; r < Rows; r++)
                    if (Get(r, trackIdx) == kind)
                    {
                        maybeIdx = r;
                        break;
                    }
        }

        return maybeIdx.HasValue ? trackCounts.TrackToPrevOzLine(maybeIdx.Value) : null;
    }
}

// ── GridItem ──────────────────────────────────────────────────────────────
// Port of taffy/src/compute/grid/types/grid_item.rs

internal sealed class GridItem
{
    public AlignItems AlignSelf;
    public float? AspectRatio;

    // Sizing contribution caches
    public Size<float?>? AvailableSpaceCache;

    // Baseline
    public float? Baseline;
    public float BaselineShim;
    public Rect<LengthPercentage> Border;
    public BoxSizing BoxSizing;
    public Line<OriginZeroLine> Column;
    public Line<ushort> ColumnIndexes;
    public bool CrossesFlexibleColumn;

    // Flexible / intrinsic track flags (set by determine_if_item_crosses)
    public bool CrossesFlexibleRow;
    public bool CrossesIntrinsicColumn;
    public bool CrossesIntrinsicRow;
    public float Height;

    // Cached style fields (avoid repeated lookups during track sizing)
    public bool IsCompressibleReplaced;
    public AlignItems JustifySelf;
    public Rect<LengthPercentageAuto> Margin;
    public Size<float?> MaxContentContributionCache;
    public Size<Dimension> MaxSizeStyle;
    public Size<float?> MinContentContributionCache;
    public Size<float?> MinimumContributionCache;
    public Size<Dimension> MinSizeStyle;
    public NodeId Node;
    public Point<Overflow> Overflow;
    public Rect<LengthPercentage> Padding;

    // Placement in OriginZero coordinates (resolved by placement algorithm)
    public Line<OriginZeroLine> Row;

    // Resolved track-vector indices (set by resolve_item_track_indexes)
    public Line<ushort> RowIndexes;
    public Size<Dimension> SizeStyle;
    public ushort SourceOrder;

    // Final position/size (set during alignment step)
    public float YPosition;

    // ── Constructor ───────────────────────────────────────────────────────

    public GridItem(NodeId node, Line<OriginZeroLine> col, Line<OriginZeroLine> row,
        Style style, AlignItems parentAlignItems, AlignItems parentJustifyItems,
        ushort sourceOrder)
    {
        Node = node;
        SourceOrder = sourceOrder;
        Row = row;
        Column = col;

        IsCompressibleReplaced = style.itemIsReplaced;
        Overflow = style.overflow;
        BoxSizing = style.boxSizing;
        SizeStyle = style.size;
        MinSizeStyle = style.minSize;
        MaxSizeStyle = style.maxSize;
        AspectRatio = style.aspectRatio;
        Padding = style.padding;
        Border = style.border;
        Margin = style.margin;
        AlignSelf = style.alignSelf ?? parentAlignItems;
        JustifySelf = style.justifySelf ?? parentJustifyItems;
        Baseline = null;
        BaselineShim = 0f;

        RowIndexes = new Line<ushort>(0, 0);
        ColumnIndexes = new Line<ushort>(0, 0);

        MinContentContributionCache = new Size<float?>(null, null);
        MaxContentContributionCache = new Size<float?>(null, null);
        MinimumContributionCache = new Size<float?>(null, null);
    }

    // ── Axis helpers ──────────────────────────────────────────────────────

    public Line<OriginZeroLine> Placement(AbstractAxis axis)
    {
        return axis == AbstractAxis.Block ? Row : Column;
    }

    public Line<ushort> PlacementIndexes(AbstractAxis axis)
    {
        return axis == AbstractAxis.Block ? RowIndexes : ColumnIndexes;
    }

    /// <summary>Range of track-vec indices that this item occupies in the given axis (excluding gutter lines).</summary>
    public (int start, int end) TrackRangeExcludingLines(AbstractAxis axis)
    {
        var indexes = PlacementIndexes(axis);
        return (indexes.Start + 1, indexes.End);
    }

    public ushort Span(AbstractAxis axis)
    {
        return Placement(axis).Span();
    }

    public bool CrossesFlexibleTrack(AbstractAxis axis)
    {
        return axis == AbstractAxis.Inline ? CrossesFlexibleColumn : CrossesFlexibleRow;
    }

    public bool CrossesIntrinsicTrack(AbstractAxis axis)
    {
        return axis == AbstractAxis.Inline ? CrossesIntrinsicColumn : CrossesIntrinsicRow;
    }

    // ── Size helpers ──────────────────────────────────────────────────────

    /// <summary>
    ///     If all spanned tracks have a definite max sizing function limit, returns the sum of those limits.
    ///     Used as an upper limit on min/max content contributions.
    /// </summary>
    public float? SpannedTrackLimit(AbstractAxis axis, List<GridTrack> axisTracks, float? axisParentSize)
    {
        var (start, end) = TrackRangeExcludingLines(axis);
        var sum = 0f;
        for (var i = start; i < end; i++)
        {
            var lim = axisTracks[i].DefiniteLimit(axisParentSize);
            if (!lim.HasValue) return null;
            sum += lim.Value;
        }

        return sum;
    }

    /// <summary>
    ///     Same as SpannedTrackLimit but excludes FitContent arguments.
    ///     Used to clamp automatic minimum contributions.
    /// </summary>
    public float? SpannedFixedTrackLimit(AbstractAxis axis, List<GridTrack> axisTracks, float? axisParentSize)
    {
        var (start, end) = TrackRangeExcludingLines(axis);
        var sum = 0f;
        for (var i = start; i < end; i++)
        {
            var lim = axisTracks[i].MaxTrackSizingFunction.DefiniteValue(axisParentSize);
            if (!lim.HasValue) return null;
            sum += lim.Value;
        }

        return sum;
    }

    /// <summary>Resolves margin sums (horizontal always 0 when container is indefinite; includes baseline shim).</summary>
    public Size<float> MarginsAxisSumsWithBaselineShims(float? innerNodeWidth)
    {
        // Horizontal margins: resolve against 0 if indefinite (per spec)
        var marginLeft = Margin.Left.IsAuto() ? 0f : Margin.Left.ResolveOrZero(0f);
        var marginRight = Margin.Right.IsAuto() ? 0f : Margin.Right.ResolveOrZero(0f);
        var marginTop = Margin.Top.IsAuto() ? 0f : Margin.Top.ResolveOrZero(innerNodeWidth);
        var marginBottom = Margin.Bottom.IsAuto() ? 0f : Margin.Bottom.ResolveOrZero(innerNodeWidth);

        return new Size<float>(marginLeft + marginRight, marginTop + marginBottom + BaselineShim);
    }

    /// <summary>
    ///     Computes the known_dimensions passed to child sizing functions.
    ///     Applies stretch alignment to fill grid area where applicable.
    /// </summary>
    public Size<float?> KnownDimensions(Size<float?> innerNodeSize, Size<float?> gridAreaSize)
    {
        var margins = MarginsAxisSumsWithBaselineShims(innerNodeSize.Width);
        var padding = Padding.ResolveOrZero(gridAreaSize);
        var border = Border.ResolveOrZero(gridAreaSize);
        var pbSum = SizeF.Add(RectF.SumAxes(padding), RectF.SumAxes(border));
        var boxAdj = BoxSizing == BoxSizing.ContentBox ? pbSum : SizeF.ZERO;

        var inherentSize = SizeF.MaybeApplyAspectRatio(
            SizeStyle.MaybeResolve(gridAreaSize).MaybeAdd(boxAdj), AspectRatio);
        var minSize = SizeF.MaybeApplyAspectRatio(
            MinSizeStyle.MaybeResolve(gridAreaSize).MaybeAdd(boxAdj), AspectRatio);
        var maxSize = SizeF.MaybeApplyAspectRatio(
            MaxSizeStyle.MaybeResolve(gridAreaSize).MaybeAdd(boxAdj), AspectRatio);

        var gridAreaMinusMargins = gridAreaSize.MaybeSub(margins);

        // Width: apply stretch if alignment is Stretch and no auto margins
        var width = inherentSize.Width;
        if (!width.HasValue && !Margin.Left.IsAuto() && !Margin.Right.IsAuto()
            && JustifySelf == AlignItems.Stretch)
            width = gridAreaMinusMargins.Width;

        var sizeAfterStretchW = SizeF.MaybeApplyAspectRatio(
            new Size<float?>(width, inherentSize.Height), AspectRatio);
        width = sizeAfterStretchW.Width;
        var height = sizeAfterStretchW.Height;

        // Height: apply stretch if alignment is Stretch and no auto margins
        if (!height.HasValue && !Margin.Top.IsAuto() && !Margin.Bottom.IsAuto()
            && AlignSelf == AlignItems.Stretch)
            height = gridAreaMinusMargins.Height;

        var finalSize = SizeF.MaybeApplyAspectRatio(new Size<float?>(width, height), AspectRatio);
        return finalSize.MaybeClamp(minSize, maxSize);
    }

    /// <summary>
    ///     Estimates available space for an item based on the other axis tracks' current sizes.
    /// </summary>
    public Size<float?> AvailableSpace(
        AbstractAxis axis,
        List<GridTrack> otherAxisTracks,
        float? otherAxisAvailableSpace,
        Func<GridTrack, float?, float?> getTrackSizeEstimate)
    {
        var (start, end) = TrackRangeExcludingLines(axis.OtherAxis());
        float? itemOtherAxisSize = 0f;
        for (var i = start; i < end; i++)
        {
            var trackEst = getTrackSizeEstimate(otherAxisTracks[i], otherAxisAvailableSpace);
            if (!trackEst.HasValue)
            {
                itemOtherAxisSize = null;
                break;
            }

            itemOtherAxisSize = itemOtherAxisSize.Value + trackEst.Value
                                                        + otherAxisTracks[i].ContentAlignmentAdjustment;
        }

        return new Size<float?>(null, null).WithAxis(axis.OtherAxis(), itemOtherAxisSize);
    }

    public Size<float?> AvailableSpaceCached(
        AbstractAxis axis,
        List<GridTrack> otherAxisTracks,
        float? otherAxisAvailableSpace,
        Func<GridTrack, float?, float?> getTrackSizeEstimate)
    {
        if (AvailableSpaceCache.HasValue) return AvailableSpaceCache.Value;
        var result = AvailableSpace(axis, otherAxisTracks, otherAxisAvailableSpace, getTrackSizeEstimate);
        AvailableSpaceCache = result;
        return result;
    }

    private static Size<AvailableSpace> MapToAvailableSpace(Size<float?> s, bool minContent)
    {
        AvailableSpace Map(float? opt)
        {
            return opt.HasValue ? Taffy.AvailableSpace.Definite(opt.Value)
                : minContent ? Taffy.AvailableSpace.MinContent : Taffy.AvailableSpace.MaxContent;
        }

        return new Size<AvailableSpace>(Map(s.Width), Map(s.Height));
    }

    /// <summary>Computes min content contribution in the given axis.</summary>
    public float MinContentContribution(AbstractAxis axis, TaffyTree tree,
        Size<float?> availableSpace, Size<float?> innerNodeSize)
    {
        var knownDims = KnownDimensions(innerNodeSize, availableSpace);
        var space = MapToAvailableSpace(availableSpace, true);
        return tree.MeasureChildSize(Node, knownDims, innerNodeSize, space,
            SizingMode.InherentSize, axis.AsAbsNaive());
    }

    public float MinContentContributionCached(AbstractAxis axis, TaffyTree tree,
        Size<float?> availableSpace, Size<float?> innerNodeSize)
    {
        var cached = MinContentContributionCache.Get(axis);
        if (cached.HasValue) return cached.Value;
        var val = MinContentContribution(axis, tree, availableSpace, innerNodeSize);
        MinContentContributionCache = MinContentContributionCache.WithAxis(axis, val);
        return val;
    }

    /// <summary>Computes max content contribution in the given axis.</summary>
    public float MaxContentContribution(AbstractAxis axis, TaffyTree tree,
        Size<float?> availableSpace, Size<float?> innerNodeSize)
    {
        var knownDims = KnownDimensions(innerNodeSize, availableSpace);
        var space = MapToAvailableSpace(availableSpace, false);
        return tree.MeasureChildSize(Node, knownDims, innerNodeSize, space,
            SizingMode.InherentSize, axis.AsAbsNaive());
    }

    public float MaxContentContributionCached(AbstractAxis axis, TaffyTree tree,
        Size<float?> availableSpace, Size<float?> innerNodeSize)
    {
        var cached = MaxContentContributionCache.Get(axis);
        if (cached.HasValue) return cached.Value;
        var val = MaxContentContribution(axis, tree, availableSpace, innerNodeSize);
        MaxContentContributionCache = MaxContentContributionCache.WithAxis(axis, val);
        return val;
    }

    /// <summary>
    ///     The minimum contribution: the smallest outer size the item can have.
    ///     See https://www.w3.org/TR/css-grid-1/#min-size-auto
    /// </summary>
    public float MinimumContribution(TaffyTree tree, AbstractAxis axis,
        List<GridTrack> axisTracks,
        Size<float?> knownDimensions, Size<float?> innerNodeSize)
    {
        var padding = Padding.ResolveOrZero(innerNodeSize);
        var border = Border.ResolveOrZero(innerNodeSize);
        var pbSum = SizeF.Add(RectF.SumAxes(padding), RectF.SumAxes(border));
        var boxAdj = BoxSizing == BoxSizing.ContentBox ? pbSum : SizeF.ZERO;

        var size = SizeStyle
            .MaybeResolve(innerNodeSize)
            .MaybeAdd(boxAdj)
            .Get(axis);
        size ??= MinSizeStyle.MaybeResolve(innerNodeSize).MaybeAdd(boxAdj).Get(axis);
        size ??= Overflow.Get(axis).MaybeIntoAutomaticMinSize();

        if (!size.HasValue)
        {
            // Compute automatic minimum size
            var (start, end) = TrackRangeExcludingLines(axis);
            var spansAutoMinTrack = false;
            var onlySpanOneTrack = end - start == 1;
            var spansFlexTrack = false;
            for (var i = start; i < end; i++)
            {
                if (axisTracks[i].MinTrackSizingFunction._cl.IsAuto()) spansAutoMinTrack = true;
                if (axisTracks[i].MaxTrackSizingFunction.IsFr()) spansFlexTrack = true;
            }

            var useContentBased = spansAutoMinTrack && (onlySpanOneTrack || !spansFlexTrack);

            if (useContentBased)
            {
                var minContent = MinContentContributionCached(axis, tree, knownDimensions, innerNodeSize);
                if (IsCompressibleReplaced)
                {
                    var styleSz = SizeStyle.Get(axis).MaybeResolve(0f);
                    var maxSz = MaxSizeStyle.Get(axis).MaybeResolve(0f);
                    minContent = minContent.MaybeMin(styleSz).MaybeMin(maxSz);
                }

                size = minContent;
            }
            else
            {
                size = 0f;
            }
        }

        var limit = SpannedFixedTrackLimit(axis, axisTracks, innerNodeSize.Get(axis));
        return size.Value.MaybeMin(limit);
    }

    public float MinimumContributionCached(TaffyTree tree, AbstractAxis axis,
        List<GridTrack> axisTracks,
        Size<float?> knownDimensions, Size<float?> innerNodeSize)
    {
        var cached = MinimumContributionCache.Get(axis);
        if (cached.HasValue) return cached.Value;
        var val = MinimumContribution(tree, axis, axisTracks, knownDimensions, innerNodeSize);
        MinimumContributionCache = MinimumContributionCache.WithAxis(axis, val);
        return val;
    }
}