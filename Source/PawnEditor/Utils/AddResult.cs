using System;
using RimWorld;
using Verse;

namespace PawnEditor;

public abstract class AddResult
{
    public static implicit operator AddResult(bool success) => success ? new SuccessInfo() : new FailureInfo();
    public static implicit operator AddResult(string reason) => new FailureInfo(reason);
    public static implicit operator AddResult(TaggedString reason) => new FailureInfo(reason);

    public static implicit operator bool(AddResult result) => result.IsSuccess();

    public abstract bool IsSuccess();

    public virtual void HandleResult(Action successAction = null)
    {
        if (IsSuccess()) successAction?.Invoke();
    }
}

public class ConfirmInfo : AddResult
{
    public readonly bool Destructive;
    public readonly AddResult Inner;
    public readonly string Key;
    public readonly bool NeedsConfirmation;
    public readonly TaggedString Text;
    public readonly string Title;

    public ConfirmInfo(TaggedString text, string key, AddResult inner, bool needsConfirmation = true, string title = null, bool destructive = false)
    {
        Text = text;
        Key = key;
        Inner = inner;
        NeedsConfirmation = needsConfirmation;
        Title = title;
        Destructive = destructive;
    }

    protected virtual bool Confirmed => !NeedsConfirmation || PawnEditorMod.Settings.DontShowAgain.Contains(Key);

    public override void HandleResult(Action successAction = null)
    {
        if (Confirmed) Inner.HandleResult(successAction);
        else Find.WindowStack.Add(new Dialog_Confirm(Text, Key, () => Inner.HandleResult(successAction), Destructive, Title));
    }

    public override bool IsSuccess() => Confirmed && Inner.IsSuccess();
}

public class FailureInfo : AddResult
{
    public readonly string Reason;

    public FailureInfo(string reason = null) => Reason = reason;

    public override bool IsSuccess() => false;

    public override void HandleResult(Action successAction = null)
    {
        if (!Reason.NullOrEmpty()) Messages.Message(Reason, MessageTypeDefOf.RejectInput, false);
    }
}

public class SuccessInfo : AddResult
{
    public readonly Action OnSuccess;

    public SuccessInfo(Action onSuccess = null) => OnSuccess = onSuccess;

    public override void HandleResult(Action successAction = null)
    {
        base.HandleResult(OnSuccess + successAction);
    }

    public override bool IsSuccess() => true;
}

public class ConditionalInfo : AddResult
{
    public readonly AddResult First;
    public readonly AddResult Second;

    public ConditionalInfo(AddResult first, AddResult second)
    {
        First = first;
        Second = second;
    }

    public override bool IsSuccess() => First.IsSuccess() && Second.IsSuccess();

    public override void HandleResult(Action successAction = null)
    {
        First.HandleResult(() => Second.HandleResult(successAction));
    }
}
