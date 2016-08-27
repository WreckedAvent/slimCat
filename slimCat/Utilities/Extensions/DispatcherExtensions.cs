using System;

namespace slimCat.Utilities.Extensions
{
    using System.Windows.Threading;

    public static class DispatcherExtensions
    {
        /// <summary>
        /// Invokes a <see cref="Dispatcher.Invoke(Action)"/> with a retry mechanism, suitable for potentially long-running tasks that might fail or be cancelled.
        /// If the number of retries is exceeded, the <see cref="Exception"/> that caused the failure will be rethrown.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="toInvoke">The action to invoke.</param>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="currentRetries">The current number of retries.</param>
        public static void InvokeWithRetry(this Dispatcher dispatcher, Action toInvoke, int maxRetries = 3, int currentRetries = 0)
        {
            try
            {
                dispatcher.Invoke(toInvoke);
            }
            catch
            {
                if (currentRetries >= maxRetries) throw;
                dispatcher.InvokeWithRetry(toInvoke, maxRetries, currentRetries + 1);
            }
        }
    }
}
