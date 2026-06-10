using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.ApplicationInsights;

public class HlaExpansionFailure
{
    public string InvalidHLA { set; get; }
    public string ExceptionType { set; get; }
    public IEnumerable<string> ExternalDonorCodes { set; get; }
    public long DonorCount { set; get; }
}