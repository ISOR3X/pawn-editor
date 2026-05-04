using Verse;

namespace PawnEditor;

public class FloatMenuDeep : FloatMenu
{
    private readonly FloatMenuDeep? _parent;
    private FloatMenuDeep? _subMenu;
    private bool _openingSubMenu;
    private bool _subMenuOptionChosen;
    private bool _closedByDistance;

    public FloatMenuDeep(List<FloatMenuOption> options, FloatMenuDeep? parent = null) : base(options)
    {
        _parent = parent;
        onlyOneOfTypeAllowed = false;
    }

    public FloatMenuDeep OpenSubMenu(List<FloatMenuOption> options)
    {
        if (_subMenu != null)
            Find.WindowStack.TryRemove(_subMenu);

        var child = new FloatMenuDeep(options, this);
        vanishIfMouseDistant = false;
        _subMenuOptionChosen = false;
        child.onCloseCallback = () =>
        {
            if (_subMenuOptionChosen)
                Find.WindowStack.TryRemove(this);
            _subMenu = null;
            _subMenuOptionChosen = false;
            if (!child._closedByDistance)
                vanishIfMouseDistant = true;
        };
        Find.WindowStack.Add(child);
        _subMenu = child;
        _openingSubMenu = true;
        return child;
    }

    public override void Close(bool doCloseSound = true)
    {
        if (!doCloseSound)
            _closedByDistance = true;
        base.Close(doCloseSound);
    }

    public override void PreOptionChosen(FloatMenuOption opt)
    {
        base.PreOptionChosen(opt);
        var p = _parent;
        while (p != null)
        {
            p._subMenuOptionChosen = true;
            p = p._parent;
        }
    }

    public override bool OnCloseRequest()
    {
        if (_openingSubMenu)
        {
            _openingSubMenu = false;
            return false;
        }
        if (_subMenu != null)
            Find.WindowStack.TryRemove(_subMenu);
        return base.OnCloseRequest();
    }
}
