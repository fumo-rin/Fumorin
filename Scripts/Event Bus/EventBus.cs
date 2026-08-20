using System;
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { }
}
namespace rinCore
{
    public static class EventBus
    {
        private static class EventHolder<T>
        {
            public static Action<T> OnEventRaised;
        }
        public static void Bind<T>(Action<T> listener)
        {
            EventHolder<T>.OnEventRaised += listener;
        }
        public static void Release<T>(Action<T> listener)
        {
            EventHolder<T>.OnEventRaised -= listener;
        }
        public static void Publish<T>(T eventData)
        {
            EventHolder<T>.OnEventRaised?.Invoke(eventData);
        }
        public static void Clear<T>()
        {
            EventHolder<T>.OnEventRaised = null;
        }
    }
}
public record CropHarvestedEvent(string CropId, int Quantity, string TileId);
public record DayPassedEvent(int CurrentDay, string Season);
public record WeatherChangedEvent(string NewWeather);