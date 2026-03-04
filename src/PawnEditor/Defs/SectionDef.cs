using System;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global

namespace PawnEditor;

[UsedImplicitly]
public class SectionDef : Def
{
    private readonly Type workerClass = typeof(SectionWorker);
    public int priority = 0;
    public float width = 1f;
    public float grow = 0f;
    public PawnUtility.PawnCategory sectionCategory = PawnUtility.PawnCategory.All;

    [field: Unsaved]
    public SectionWorker Worker
    {
        get
        {
            if (field != null) return field;
            field = (SectionWorker)Activator.CreateInstance(workerClass, this);
            field.Def = this;

            return field;
        }
    }
    public override void ResolveReferences()
    {
        base.ResolveReferences();
        width = Mathf.Clamp(width, 0f, 1f);
    }
}