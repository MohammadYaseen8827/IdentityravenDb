using Platform.Application.Interfaces;

namespace Platform.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime NowUtc => DateTime.UtcNow;

}
