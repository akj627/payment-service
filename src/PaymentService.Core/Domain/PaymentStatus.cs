namespace PaymentService.Core.Domain;

public enum PaymentStatus
{
    Pending,      // waiting to be sent (first attempt or a scheduled retry)
    Dispatching,  // claimed by a worker; the bank call may be in flight
    Submitted,    // bank accepted it (final)
    Failed,       // bank rejected it (final, no money moved)
    NeedsReview,  // retries ran out and the outcome is unknown (final, a person must check with the bank)
}

public enum BatchStatus
{
    Processing,             // at least one payment is still Pending or Dispatching
    Completed,              // every payment was Submitted
    CompletedWithFailures,  // everything finished, but some payments Failed or NeedsReview
}
