using System.Data;
using Microsoft.EntityFrameworkCore;

namespace t1_frame.entityframeworkcore.abp
{
    /// <summary>
    /// 库存扣减 / 商品销售示例的建表与初始化数据。
    /// 主项目没有为 T1FrameAbpDbContext 配置迁移，这里用幂等 DDL 补齐演示表。
    /// </summary>
    public static class InventorySchema
    {
        public const string DemoProductName = "库存扣减演示商品";

        private const string CreateStockFlowTableSql = @"
CREATE TABLE IF NOT EXISTS `t1_inventory_stock_flow` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `biz_no` varchar(64) NOT NULL,
  `product_id` bigint NOT NULL,
  `product_name` varchar(100) NOT NULL,
  `quantity` int NOT NULL,
  `source` varchar(32) NOT NULL,
  `remaining_stock` int NOT NULL,
  `created_at` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_t1_inventory_stock_flow_biz_no` (`biz_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        private const string CreateOrderTableSql = @"
CREATE TABLE IF NOT EXISTS `t1_inventory_order` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `order_no` varchar(64) NOT NULL,
  `sale_no` varchar(64) NULL,
  `product_id` bigint NOT NULL,
  `quantity` int NOT NULL,
  `unit_price` decimal(18,2) NOT NULL DEFAULT 0,
  `customer_name` varchar(100) NULL,
  `status` int NOT NULL,
  `created_at` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_t1_inventory_order_order_no` (`order_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        private const string CreateProductDetailTableSql = @"
CREATE TABLE IF NOT EXISTS `t1_inventory_product_detail` (
  `Id` bigint NOT NULL,
  `price` decimal(18,2) NOT NULL DEFAULT 0,
  `description` varchar(500) NULL,
  `image_url` varchar(500) NULL,
  `category` varchar(50) NULL,
  `is_on_sale` tinyint(1) NOT NULL DEFAULT 0,
  `created_at` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        private static readonly CatalogItem[] Catalog =
        {
            new()
            {
                Name = DemoProductName,
                Stock = 100,
                Price = 5999m,
                Category = "手机",
                Description = "A16 芯片，6.1 英寸 OLED，库存扣减演示商品。",
                ImageUrl = "https://images.unsplash.com/photo-1592750475338-74b7b21085ab?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Name = "机械键盘 K87",
                Stock = 50,
                Price = 499m,
                Category = "外设",
                Description = "87 键热插拔，PBT 键帽，适合长时间编码。",
                ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Name = "降噪耳机 H1",
                Stock = 80,
                Price = 899m,
                Category = "音频",
                Description = "主动降噪，40 小时续航，通勤与办公。",
                ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=900&q=80"
            },
            new()
            {
                Name = "便携显示器 M16",
                Stock = 30,
                Price = 1299m,
                Category = "显示",
                Description = "16 英寸 2.5K 便携屏，一线连接笔记本。",
                ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?auto=format&fit=crop&w=900&q=80"
            }
        };

        public static void Initialize(T1FrameAbpDbContext db)
        {
            db.Database.ExecuteSqlRaw(CreateStockFlowTableSql);
            db.Database.ExecuteSqlRaw(CreateOrderTableSql);
            db.Database.ExecuteSqlRaw(CreateProductDetailTableSql);

            // 兼容之前已经建好的 t1_inventory_order
            if (!ColumnExists(db, "t1_inventory_order", "sale_no"))
            {
                db.Database.ExecuteSqlRaw(
                    "ALTER TABLE `t1_inventory_order` ADD COLUMN `sale_no` varchar(64) NULL AFTER `order_no`;");
            }

            if (!ColumnExists(db, "t1_inventory_order", "unit_price"))
            {
                db.Database.ExecuteSqlRaw(
                    "ALTER TABLE `t1_inventory_order` ADD COLUMN `unit_price` decimal(18,2) NOT NULL DEFAULT 0 AFTER `quantity`;");
            }

            if (!ColumnExists(db, "t1_inventory_order", "customer_name"))
            {
                db.Database.ExecuteSqlRaw(
                    "ALTER TABLE `t1_inventory_order` ADD COLUMN `customer_name` varchar(100) NULL AFTER `unit_price`;");
            }

            SeedCatalog(db);
        }

        private static void SeedCatalog(T1FrameAbpDbContext db)
        {
            foreach (var item in Catalog)
            {
                var product = db.T1GoodsStock
                    .FirstOrDefault(x => x.goods_name == item.Name);

                if (product == null)
                {
                    product = new T1GoodsStock
                    {
                        goods_name = item.Name,
                        goods_stock = item.Stock
                    };

                    db.T1GoodsStock.Add(product);
                    db.SaveChanges();
                }

                if (db.T1InventoryProductDetail.Any(x => x.Id == product.Id))
                {
                    continue;
                }

                db.T1InventoryProductDetail.Add(new T1InventoryProductDetail(product.Id)
                {
                    price = item.Price,
                    category = item.Category,
                    description = item.Description,
                    image_url = item.ImageUrl,
                    is_on_sale = true,
                    created_at = DateTime.Now
                });

                db.SaveChanges();
            }
        }

        private static bool ColumnExists(T1FrameAbpDbContext db, string table, string column)
        {
            var connection = db.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT COUNT(*) FROM information_schema.columns " +
                    "WHERE table_schema = DATABASE() AND table_name = @table AND column_name = @column";

                var tableParameter = command.CreateParameter();
                tableParameter.ParameterName = "@table";
                tableParameter.Value = table;
                command.Parameters.Add(tableParameter);

                var columnParameter = command.CreateParameter();
                columnParameter.ParameterName = "@column";
                columnParameter.Value = column;
                command.Parameters.Add(columnParameter);

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        private sealed class CatalogItem
        {
            public string Name { get; init; } = string.Empty;

            public int Stock { get; init; }

            public decimal Price { get; init; }

            public string Category { get; init; } = string.Empty;

            public string Description { get; init; } = string.Empty;

            public string ImageUrl { get; init; } = string.Empty;
        }
    }
}
