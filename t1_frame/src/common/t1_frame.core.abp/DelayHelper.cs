using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.core.abp
{
    public class DelayHelper
    {
        public static async Task<bool> DoWorkWithTimeoutAsync(TimeSpan delay, TimeSpan timeout, CancellationToken externalToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                externalToken,
                timeoutCts.Token);

            try
            {
                await Task.Delay(delay, linkedCts.Token);
                return true; // 成功完成
            }
            catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
            {
                Console.WriteLine("⏱️ 超时取消");
                //return false;
                throw new Exception("⏱️ 超时取消", ex);
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("🛑 外部请求取消");
                throw new Exception("🛑 外部请求取消", ex); ;
            }
        }
    }
}
