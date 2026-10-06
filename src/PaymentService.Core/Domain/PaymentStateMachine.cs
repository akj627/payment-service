using Stateless;
using Stateless.Graph;

namespace PaymentService.Core.Domain;

public enum PaymentTrigger
{
    StartDispatch,
    BankAccepted,
    BankRejected,
    ScheduleRetry,
    EscalateForReview,
}

/// <summary>
/// The only place that defines which payment status changes are allowed.
///
///   Pending ──StartDispatch──▶ Dispatching ──BankAccepted──────▶ Submitted
///      ▲                           │ ──BankRejected──────▶ Failed
///      └────────ScheduleRetry──────┘ ──EscalateForReview──▶ NeedsReview
/// </summary>
public static class PaymentStateMachine
{
    public static StateMachine<PaymentStatus, PaymentTrigger> Create(
        Func<PaymentStatus> readStatus,
        Action<PaymentStatus> writeStatus)
    {
        var machine = new StateMachine<PaymentStatus, PaymentTrigger>(readStatus, writeStatus);

        machine.Configure(PaymentStatus.Pending)
            .Permit(PaymentTrigger.StartDispatch, PaymentStatus.Dispatching);

        machine.Configure(PaymentStatus.Dispatching)
            .Permit(PaymentTrigger.BankAccepted, PaymentStatus.Submitted)
            .Permit(PaymentTrigger.BankRejected, PaymentStatus.Failed)
            .Permit(PaymentTrigger.ScheduleRetry, PaymentStatus.Pending)
            .Permit(PaymentTrigger.EscalateForReview, PaymentStatus.NeedsReview);

        // Submitted, Failed and NeedsReview are final: nothing is permitted from them.

        machine.OnUnhandledTrigger((status, trigger) =>
            throw new DomainException($"A payment in status {status} cannot {trigger}."));

        return machine;
    }

    /// <summary>The configured machine as a Mermaid diagram, used in the architecture doc.</summary>
    public static string ToMermaidDiagram()
    {
        var status = PaymentStatus.Pending;
        var machine = Create(() => status, s => status = s);
        return MermaidGraph.Format(machine.GetInfo());
    }
}
