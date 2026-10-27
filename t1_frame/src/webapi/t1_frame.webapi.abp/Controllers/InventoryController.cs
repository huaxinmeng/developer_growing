using Microsoft.AspNetCore.Mvc;
using t1_frame.application.abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace t1_frame.webapi.abp.Controllers
{
    /// <summary>
    /// 库存中心 REST API（给 Vue 前端使用）。
    /// </summary>
    [ApiController]
    [Route("api/inventory")]
    public class InventoryController : AbpController
    {
        private readonly IInventoryAppService _inventoryAppService;

        public InventoryController(IInventoryAppService inventoryAppService)
        {
            _inventoryAppService = inventoryAppService;
        }

        [HttpGet]
        public Task<InventoryProductDto> GetAsync()
        {
            return _inventoryAppService.GetAsync();
        }

        [HttpGet("dashboard")]
        public Task<InventoryDashboardDto> GetDashboardAsync()
        {
            return _inventoryAppService.GetDashboardAsync();
        }

        [HttpGet("products")]
        public Task<PagedResultDto<InventoryProductDto>> GetProductsAsync()
        {
            return _inventoryAppService.GetProductsAsync();
        }

        [HttpPost("products")]
        public Task<InventoryProductDto> CreateProductAsync([FromBody] InventoryCreateProductInput input)
        {
            return _inventoryAppService.CreateProductAsync(input);
        }

        [HttpPost("products/{productId:long}/stock")]
        public Task<InventoryProductDto> SetStockAsync(long productId, [FromQuery] int stock)
        {
            return _inventoryAppService.SetStockAsync(productId, stock);
        }

        [HttpPut("products/{productId:long}")]
        public Task<InventoryProductDto> UpdateProductAsync(
            long productId,
            [FromBody] InventoryUpdateProductInput input)
        {
            return _inventoryAppService.UpdateProductAsync(productId, input);
        }

        [HttpPost("sales")]
        public Task<InventorySaleResultDto> CreateSaleAsync([FromBody] InventoryCreateSaleInput input)
        {
            return _inventoryAppService.CreateSaleAsync(input);
        }

        [HttpPost("orders/{orderId:long}/pay")]
        public Task<InventoryOrderDto> PayOrderAsync(long orderId)
        {
            return _inventoryAppService.PayOrderAsync(orderId);
        }

        [HttpPost("orders/{orderId:long}/cancel")]
        public Task<InventorySaleGroupDto> CancelOrderAsync(long orderId)
        {
            return _inventoryAppService.CancelOrderAsync(orderId);
        }

        [HttpGet("sale-orders")]
        public Task<PagedResultDto<InventorySaleGroupDto>> GetSaleOrdersAsync(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20,
            [FromQuery] int? status = null)
        {
            return _inventoryAppService.GetSaleOrdersAsync(skip, take, status);
        }

        [HttpPost("sale-orders/{saleNo}/pay")]
        public Task<InventorySaleGroupDto> PaySaleAsync(string saleNo)
        {
            return _inventoryAppService.PaySaleAsync(saleNo);
        }

        [HttpPost("sale-orders/{saleNo}/cancel")]
        public Task<InventorySaleGroupDto> CancelSaleAsync(string saleNo)
        {
            return _inventoryAppService.CancelSaleAsync(saleNo);
        }

        [HttpGet("orders")]
        public Task<PagedResultDto<InventoryOrderDto>> GetOrdersAsync(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20,
            [FromQuery] int? status = null)
        {
            return _inventoryAppService.GetOrdersAsync(skip, take, status);
        }

        [HttpGet("stock-flows")]
        public Task<PagedResultDto<InventoryStockFlowDto>> GetStockFlowsAsync(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            return _inventoryAppService.GetStockFlowsAsync(skip, take);
        }

        [HttpGet("audit-logs")]
        public Task<PagedResultDto<InventoryAuditLogDto>> GetAuditLogsAsync([FromQuery] int take = 50)
        {
            return _inventoryAppService.GetAuditLogsAsync(take);
        }

        [HttpPost("reset")]
        public Task<InventoryProductDto> ResetAsync([FromQuery] int stock = 100)
        {
            return _inventoryAppService.ResetAsync(stock);
        }

        [HttpPost("deduct/naive")]
        public Task<InventoryDeductResultDto> DeductNaiveAsync([FromQuery] int quantity = 1)
        {
            return _inventoryAppService.DeductNaiveAsync(quantity);
        }

        [HttpPost("deduct/atomic")]
        public Task<InventoryDeductResultDto> DeductAtomicAsync([FromQuery] int quantity = 1)
        {
            return _inventoryAppService.DeductAtomicAsync(quantity);
        }

        [HttpPost("deduct/optimistic")]
        public Task<InventoryDeductResultDto> DeductOptimisticAsync([FromQuery] int quantity = 1)
        {
            return _inventoryAppService.DeductOptimisticAsync(quantity);
        }

        [HttpPost("deduct/redis")]
        public Task<InventoryDeductResultDto> DeductWithRedisAsync([FromQuery] int quantity = 1)
        {
            return _inventoryAppService.DeductWithRedisAsync(quantity);
        }

        [HttpPost("deduct/idempotent")]
        public Task<InventoryDeductResultDto> DeductIdempotentAsync(
            [FromQuery] string bizNo,
            [FromQuery] int quantity = 1)
        {
            return _inventoryAppService.DeductIdempotentAsync(bizNo, quantity);
        }

        [HttpPost("stress")]
        public Task<InventoryConcurrencyResultDto> RunConcurrencyTestAsync(
            [FromQuery] string mode = "atomic",
            [FromQuery] int requests = 100,
            [FromQuery] int quantity = 1,
            [FromQuery] int initialStock = 100,
            [FromQuery] int parallelism = 8)
        {
            return _inventoryAppService.RunConcurrencyTestAsync(
                mode, requests, quantity, initialStock, parallelism);
        }
    }
}
