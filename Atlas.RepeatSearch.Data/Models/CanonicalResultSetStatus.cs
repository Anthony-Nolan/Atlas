namespace Atlas.RepeatSearch.Data.Models;

public enum CanonicalResultSetStatus
{
    InProgress = 0,
    Complete = 1,

    /// <summary>
    /// The retention job is deleting the rows of the set, in batches. Repeat searches do not use the set, so none of them reads it while
    /// it is only partly deleted. A set is marked in the same statement that checks that it is still expired.
    /// </summary>
    Deleting = 2
}
