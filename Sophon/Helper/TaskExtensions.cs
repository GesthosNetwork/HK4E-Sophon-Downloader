using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sophon.Helper
{
    internal delegate Task<TResult> ActionTimeoutTaskCallback<TResult>(CancellationToken token);
    internal delegate void ActionOnTimeOutRetry(int retryAttemptCount, int retryAttemptTotal, int timeOutSecond, int timeOutStep);

    internal static class TaskExtensions
    {
        internal const int DefaultTimeoutSec = 20;
        internal const int DefaultRetryAttempt = 10;

        internal static async Task<TResult> WaitForRetryAsync<TResult>(
            Func<ActionTimeoutTaskCallback<TResult>> funcCallback,
            int? timeout = null,
            int? timeoutStep = null,
            int? retryAttempt = null,
            ActionOnTimeOutRetry actionOnRetry = null,
            CancellationToken fromToken = default)
        {
            timeout ??= DefaultTimeoutSec;
            timeoutStep ??= 0;
            retryAttempt ??= DefaultRetryAttempt;

            int retryAttemptCurrent = 1;
            Exception lastException = null;

            while (retryAttemptCurrent < retryAttempt)
            {
                fromToken.ThrowIfCancellationRequested();
                CancellationTokenSource innerCancellationToken = null;
                CancellationTokenSource linkedToken = null;

                try
                {
                    innerCancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(timeout.Value));
                    linkedToken = CancellationTokenSource.CreateLinkedTokenSource(innerCancellationToken.Token, fromToken);

                    var callback = funcCallback();
                    return await callback(linkedToken.Token);
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    actionOnRetry?.Invoke(retryAttemptCurrent, retryAttempt.Value, timeout.Value, timeoutStep.Value);

                    if (ex is TimeoutException)
                    {
                        Logger.PushLogWarning(null, $"The operation has timed out! Retrying attempt {retryAttemptCurrent}/{retryAttempt}");
                    }
                    else
                    {
                        Logger.PushLogError(null, $"The operation has thrown an exception! Retrying attempt {retryAttemptCurrent}/{retryAttempt}\r\n{ex}");
                    }

                    retryAttemptCurrent++;
                    timeout += timeoutStep;
                }
                finally
                {
                    innerCancellationToken?.Dispose();
                    linkedToken?.Dispose();
                }
            }

            if (lastException != null && !fromToken.IsCancellationRequested)
            {
                throw lastException is TaskCanceledException
                    ? new TimeoutException("The operation has timed out with inner exception!", lastException)
                    : lastException;
            }

            throw new TimeoutException("The operation has timed out!");
        }
    }
}