using System.Collections.Specialized;

namespace GridDomain.Scheduling.Quartz.Configuration
{
    //TODO: refactor to avoid only on of properties usage in implementation
    public interface IQuartzConfig
    {
        NameValueCollection Settings { get; }
        string Name { get; }
    }
}