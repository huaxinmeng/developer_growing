namespace t1_frame.application.abp
{
    public class InventoryProductDto
    {
        public long Id { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Stock { get; set; }

        public decimal Price { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public string? Category { get; set; }

        public bool IsOnSale { get; set; }
    }

    public class InventoryDeductResultDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int RemainingStock { get; set; }

        public int Attempts { get; set; }

        public string BizNo { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 扣减成功后是否成功投递到分布式事件总线（RabbitMQ / Kafka）。
        /// </summary>
        public bool EventPublished { get; set; }
    }

    public class InventoryConcurrencyResultDto
    {
        public string Mode { get; set; } = string.Empty;

        public int Requests { get; set; }

        /// <summary>
        /// 实际并发度。MySQL max_connections 较小时用它限制连接数。
        /// </summary>
        public int Parallelism { get; set; }

        public int Quantity { get; set; }

        public int InitialStock { get; set; }

        public int FinalStock { get; set; }

        public int SuccessCount { get; set; }

        public int FailCount { get; set; }

        public int DeductedQuantity { get; set; }

        /// <summary>
        /// 成功订单数 × 数量 - 实际扣减；大于 0 表示订单成功但库存没扣够（超卖风险）。
        /// </summary>
        public int LostUpdates { get; set; }

        public bool Oversold => FinalStock < 0;

        public long ElapsedMs { get; set; }

        public string Note { get; set; } = string.Empty;
    }

    public class InventoryAuditLogDto
    {
        public string BizNo { get; set; } = string.Empty;

        public long ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int RemainingStock { get; set; }

        public string Source { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    public class InventoryCreateProductInput
    {
        public string ProductName { get; set; } = string.Empty;

        public int Stock { get; set; }

        public decimal Price { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public string? Category { get; set; }

        public bool IsOnSale { get; set; } = true;
    }

    public class InventoryUpdateProductInput
    {
        public decimal Price { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public string? Category { get; set; }

        public bool IsOnSale { get; set; }
    }

    public class InventoryOrderDto
    {
        public long Id { get; set; }

        public string OrderNo { get; set; } = string.Empty;

        public string SaleNo { get; set; } = string.Empty;

        public long ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalAmount { get; set; }

        public string? CustomerName { get; set; }

        public int Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class InventorySaleGroupDto
    {
        /// <summary>
        /// 销售单号；库存演示订单没有销售单号时使用订单号。
        /// </summary>
        public string SaleNo { get; set; } = string.Empty;

        public string? CustomerName { get; set; }

        public int Status { get; set; }

        public decimal TotalAmount { get; set; }

        public int TotalQuantity { get; set; }

        public int OrderCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<InventoryOrderDto> Items { get; set; } = new();
    }

    public class InventoryStockFlowDto
    {
        public long Id { get; set; }

        public string BizNo { get; set; } = string.Empty;

        public long ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int RemainingStock { get; set; }

        public string Source { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    public class InventoryDashboardDto
    {
        public int ProductCount { get; set; }

        public int TotalStock { get; set; }

        public int LowStockCount { get; set; }

        public int OrderCount { get; set; }
    }

    public class InventorySaleItemInput
    {
        public long ProductId { get; set; }

        public int Quantity { get; set; }
    }

    public class InventoryCreateSaleInput
    {
        /// <summary>
        /// 客户端请求号，用于幂等；为空时服务端生成。
        /// </summary>
        public string? RequestId { get; set; }

        public string? CustomerName { get; set; }

        public List<InventorySaleItemInput> Items { get; set; } = new();
    }

    public class InventorySaleItemResultDto
    {
        public long ProductId { get; set; }

        public string OrderNo { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalAmount { get; set; }

        public int RemainingStock { get; set; }
    }

    public class InventorySaleResultDto
    {
        public bool Success { get; set; }

        public string SaleNo { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public List<InventorySaleItemResultDto> Items { get; set; } = new();
    }
}
