namespace Project.Runtime.Logic
{
    /// <summary>
    /// Abstracts analytics/telemetry reporting.
    /// Production: sends to backend. Tests: mock or spy to assert calls.
    /// </summary>
    public interface IAnalyticsService
    {
        void TrackEvent(string eventName);
        void TrackEvent(string eventName, string key, object value);
    }
}
