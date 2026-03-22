namespace FlexLayout
{
    /// <summary>
    /// Determines which layout algorithm a container uses for its children.
    /// Mirrors the CSS <c>display</c> property.
    /// </summary>
    public enum Display
    {
        Block, // default — no layout algorithm applied to children (CSS default)
        Flex, // children are laid out using the flexbox algorithm
    }

    /// <summary>
    /// Unified style for any layout element or container, mirroring how CSS applies
    /// properties to elements regardless of their role. Properties irrelevant to a
    /// given context (e.g. flexDirection on a leaf item) are simply ignored by the solver.
    /// </summary>
    public struct ElementStyle
    {
        // ── Item sizing ────────────────────────────────────────────────────────

        public float grow;
        public float shrink;
        public StyleLength basis;
        public StyleLength width;
        public StyleLength height;
        public StyleLength minWidth;
        public StyleLength minHeight;
        public StyleLength maxWidth;
        public StyleLength maxHeight;
        public int order;
        public Display display;

        // ── Container config ───────────────────────────────────────────────────

        public FlexDirection flexDirection;
        public FlexWrap flexWrap;
        public float columnGap;
        public float rowGap;

        // ── Constructor ────────────────────────────────────────────────────────

        private ElementStyle(
            float? grow, float? shrink,
            StyleLength? basis, StyleLength? width, StyleLength? height,
            StyleLength? minWidth, StyleLength? minHeight,
            StyleLength? maxWidth, StyleLength? maxHeight,
            int order,
            FlexDirection flexDirection, FlexWrap flexWrap,
            float columnGap, float rowGap,
            Display display = Display.Flex)
        {
            this.grow = grow ?? 0f;
            this.shrink = shrink ?? 1f;
            this.basis = basis ?? StyleLength.Auto();
            this.width = width ?? StyleLength.Auto();
            this.height = height ?? StyleLength.Auto();
            this.minWidth = minWidth ?? StyleLength.Px(0f);
            this.minHeight = minHeight ?? StyleLength.Px(0f);
            this.maxWidth = maxWidth ?? StyleLength.Auto();
            this.maxHeight = maxHeight ?? StyleLength.Auto();
            this.order = order;
            this.flexDirection = flexDirection;
            this.flexWrap = flexWrap;
            this.columnGap = columnGap;
            this.rowGap = rowGap;
            this.display = display;
        }

        // ── Copy-with helper ───────────────────────────────────────────────────

        public ElementStyle With(
            float? grow = null,
            float? shrink = null,
            StyleLength? basis = null,
            StyleLength? width = null,
            StyleLength? height = null,
            StyleLength? minWidth = null,
            StyleLength? minHeight = null,
            StyleLength? maxWidth = null,
            StyleLength? maxHeight = null,
            int? order = null,
            FlexDirection? flexDirection = null,
            FlexWrap? flexWrap = null,
            float? columnGap = null,
            float? rowGap = null,
            Display? display = null)
        {
            return new ElementStyle(
                grow ?? this.grow,
                shrink ?? this.shrink,
                basis ?? this.basis,
                width ?? this.width,
                height ?? this.height,
                minWidth ?? this.minWidth,
                minHeight ?? this.minHeight,
                maxWidth ?? this.maxWidth,
                maxHeight ?? this.maxHeight,
                order ?? this.order,
                flexDirection ?? this.flexDirection,
                flexWrap ?? this.flexWrap,
                columnGap ?? this.columnGap,
                rowGap ?? this.rowGap,
                display ?? this.display);
        }
        
        // ── Presets ───────────────────────────────────────────────────────────

        /// <summary>Grows to fill available space. Equivalent to <c>flex-grow: 1</c>.</summary>
        public static ElementStyle Fill =>
            new(1f, null, null, null, null, null, null, null, null, 0, default, default, 0f, 0f);

        /// <summary>Fixed pixel size, does not grow or shrink.</summary>
        public static ElementStyle Fixed(float width, float height)
            => new(0f, 0f, null, StyleLength.Px(width), StyleLength.Px(height), null, null, null, null, 0, default,
                default, 0f, 0f);

        /// <summary>Fixed width, does not shrink.</summary>
        public static ElementStyle FixedWidth(float width)
            => new(0f, 0f, null, StyleLength.Px(width), null, null, null, null, null, 0, default, default, 0f, 0f);

        /// <summary>Fixed height, does not shrink.</summary>
        public static ElementStyle FixedHeight(float height)
            => new(0f, 0f, null, null, StyleLength.Px(height), null, null, null, null, 0, default, default, 0f, 0f);

        /// <summary>Row container preset.</summary>
        public static ElementStyle Row(float gap = 0f)
            => new(0f, null, null, null, null, null, null, null, null, 0, FlexDirection.Row, default, gap, 0f,
                Display.Flex);

        /// <summary>Column container preset.</summary>
        public static ElementStyle Column(float gap = 0f)
            => new(0f, null, null, null, null, null, null, null, null, 0, FlexDirection.Column, default, 0f, gap,
                Display.Flex);
    }
}