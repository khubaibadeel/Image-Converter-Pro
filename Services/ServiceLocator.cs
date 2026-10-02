using System;

namespace ImageConverterPro.Services
{
    public static class ServiceLocator
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public static T GetService<T>() where T : class
        {
            if (ServiceProvider == null)
            {
                throw new InvalidOperationException("ServiceLocator has not been initialized. Call Initialize() first.");
            }

            var service = ServiceProvider.GetService(typeof(T)) as T;
            return service ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }
    }
}
