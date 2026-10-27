using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace t1_frame.application.abp
{
    /// <summary>
    /// 库存扣减示例（MySQL + Redis + MQ + MongoDB）。
    /// 约定路由前缀为 /app/inventory。
    /// </summary>
    public interface IInventoryAppService : IApplicationService
    {
        Task<InventoryProductDto> GetAsync();

        Task<InventoryProductDto> ResetAsync(int stock = 100);

        /// <summary>错误示范：读-改-写，存在丢失更新。</summary>
        Task<InventoryDeductResultDto> DeductNaiveAsync(int quantity = 1);

        /// <summary>推荐方案：MySQL 条件更新，判断与扣减原子完成。</summary>
        Task<InventoryDeductResultDto> DeductAtomicAsync(int quantity = 1);

        /// <summary>乐观锁：用库存快照做 CAS，冲突后重试。</summary>
        Task<InventoryDeductResultDto> DeductOptimisticAsync(int quantity = 1);

        /// <summary>Redis Lua 预扣 + MySQL 落库 + 失败回补。</summary>
        Task<InventoryDeductResultDto> DeductWithRedisAsync(int quantity = 1);

        /// <summary>Redis SETNX + MySQL 唯一索引双重幂等。</summary>
        Task<InventoryDeductResultDto> DeductIdempotentAsync(string bizNo, int quantity = 1);

        /// <summary>并发压测：mode = naive | atomic | optimistic | redis。</summary>
        Task<InventoryConcurrencyResultDto> RunConcurrencyTestAsync(
            string mode = "atomic",
            int requests = 100,
            int quantity = 1,
            int initialStock = 100,
            int parallelism = 8);

        /// <summary>读取 MongoDB 中由 MQ 消费者写入的审计日志。</summary>
        Task<PagedResultDto<InventoryAuditLogDto>> GetAuditLogsAsync(int take = 20);

        // ---------- 管理端接口 ----------

        Task<PagedResultDto<InventoryProductDto>> GetProductsAsync();

        Task<InventoryProductDto> CreateProductAsync(InventoryCreateProductInput input);

        Task<InventoryProductDto> SetStockAsync(long productId, int stock);

        Task<InventoryProductDto> UpdateProductAsync(
            long productId,
            InventoryUpdateProductInput input);

        /// <summary>商店下单：多商品结算，库存扣减与订单写入同一事务。</summary>
        Task<InventorySaleResultDto> CreateSaleAsync(InventoryCreateSaleInput input);

        Task<InventoryOrderDto> PayOrderAsync(long orderId);

        Task<PagedResultDto<InventoryOrderDto>> GetOrdersAsync(
            int skip = 0,
            int take = 20,
            int? status = null);

        /// <summary>按销售单分组的订单列表。</summary>
        Task<PagedResultDto<InventorySaleGroupDto>> GetSaleOrdersAsync(
            int skip = 0,
            int take = 20,
            int? status = null);

        /// <summary>支付整张销售单。</summary>
        Task<InventorySaleGroupDto> PaySaleAsync(string saleNo);

        /// <summary>取消整张销售单并回补库存。</summary>
        Task<InventorySaleGroupDto> CancelSaleAsync(string saleNo);

        /// <summary>取消订单；若属于销售单，则取消整张销售单并回补库存。</summary>
        Task<InventorySaleGroupDto> CancelOrderAsync(long orderId);

        Task<PagedResultDto<InventoryStockFlowDto>> GetStockFlowsAsync(
            int skip = 0,
            int take = 20);

        Task<InventoryDashboardDto> GetDashboardAsync();
    }
}
