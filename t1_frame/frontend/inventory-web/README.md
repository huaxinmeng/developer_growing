# 库存控制台前端

Vue 3 + Vite + TypeScript + Element Plus，对接 `t1_frame.webapi.abp` 的库存扣减 API。

## 页面

- **商品商店**：在售商品网格、分类/搜索、商品详情、购物车、下单、下单成功回执。
- **库存管理**：库存总览、商品搜索、新建商品、编辑价格/描述/图片/分类、上下架、调整库存、快速扣减（条件更新 / 乐观锁 / Redis / 错误示范）、并发压测。
- **订单管理**：按销售单/按明细双视图、状态筛选、分页、客户/金额、支付整单、取消整单并回补库存。
- **审计日志**：MongoDB 消息审计 + MySQL 库存流水双视图，来源筛选、自动刷新、分页。

商品商店与库存管理共用同一份商品数据：后台改价格、描述、图片或上下架，商店立即生效。
下单会真实扣减 MySQL 库存并生成待支付订单；支付按钮只演示状态流转，不接真实支付网关。

### 换肤

顶栏的“主题”菜单内置 11 套主题，色板来自 `theme-factory` skill：

- 库存台账（默认）、深海、日落大道、森林冠层、现代极简、金色时刻
- 冰霜、沙漠玫瑰、科技创新、植物园、午夜星河

主题通过 `data-theme` + CSS 变量切换，持久化在 `localStorage` 的 `inventory-theme`。
同时覆盖 Element Plus 的 primary / surface / text / border 变量，弹窗、表格、下拉、输入框都会跟随。
标题字体按主题使用 Bricolage Grotesque、Fraunces 或 Space Grotesk。

### 多语言

支持 **简体中文 / English**，顶栏“ZH / EN”切换，持久化在 `localStorage` 的 `inventory-locale`。

- 使用 `vue-i18n` + Element Plus `ElConfigProvider`，分页、日期等组件文案同步切换
- 页面标题、按钮、表格、弹窗、确认框、Toast、状态和来源标签全部翻译
- 后端常见业务错误（库存不足、已下架、重复请求、订单状态变化等）做了中英映射
- 商品名称/描述是数据库内容，保持原样；商品分类做了一层中英显示映射

语言与主题初始化在 `src/i18n/index.ts`、`src/themes.ts`，刷新后保持上次选择。

## 运行

先启动后端（默认 `http://localhost:5288`）：

```powershell
dotnet run --project t1_frame/src/webapi/t1_frame.webapi.abp/t1_frame.webapi.abp.csproj --launch-profile http
```

再启动前端：

```powershell
cd t1_frame/frontend/inventory-web
npm install
npm run dev
```

打开 <http://localhost:5173>。

Vite 已配置代理：`/api` → `http://localhost:5288`，开发环境不需要额外处理跨域。后端也配置了
`http://localhost:5173` / `http://localhost:4173` 的 CORS 策略。

## 构建

```powershell
npm run build
npm run preview
```

## 技术说明

- API 客户端：`src/api/http.ts`、`src/api/inventory.ts`
- 类型定义：`src/types.ts`
- 路由：`/shop`、`/inventory`、`/orders`、`/audit`
- 页面：`src/views/`
- 主题元数据：`src/themes.ts`
- 中英文案：`src/i18n/messages/`
- 后端错误中英映射：`src/i18n/server.ts`
- 设计变量与 Element Plus 主题覆盖：`src/styles/main.css`

设计方向是“仓库控制台 / 库存台账”：冷灰绿纸面、墨绿侧栏、青绿主色、锈红告警；
标题使用 Bricolage Grotesque，数据与时间使用 IBM Plex Mono；表格与面板使用描边而不是卡片阴影。

## 截图

![商品商店](../../../docs/assets/inventory-web-shop.png)

![购物车](../../../docs/assets/inventory-web-cart.png)

![下单成功](../../../docs/assets/inventory-web-sale-success.png)

![主题菜单](../../../docs/assets/inventory-web-theme-selector.png)

![深海主题 + 英文](../../../docs/assets/inventory-web-theme-ocean.png)

![科技主题](../../../docs/assets/inventory-web-theme-tech.png)

![日落主题 + 编辑弹窗](../../../docs/assets/inventory-web-theme-sunset.png)

![英文订单](../../../docs/assets/inventory-web-i18n-orders.png)

![英文审计日志](../../../docs/assets/inventory-web-i18n-audit.png)

![库存管理](../../../docs/assets/inventory-web-inventory.png)

![编辑商品](../../../docs/assets/inventory-web-edit-product.png)

![订单管理（按销售单分组）](../../../docs/assets/inventory-web-orders.png)

![取消后回补库存](../../../docs/assets/inventory-web-orders-cancelled.png)

![订单明细视图](../../../docs/assets/inventory-web-orders-flat.png)

![审计日志](../../../docs/assets/inventory-web-audit.png)

![并发压测](../../../docs/assets/inventory-web-stress.png)

## 1000 QPS 秒杀通道

前端之外，后端另提供独立的秒杀入口：

```text
POST /api/seckill/{productId}?quantity=1&requestId=xxx
GET  /api/seckill/status/{requestId}
GET  /api/seckill/stats?productId=2
POST /api/seckill/reset?productId=2&stock=10000
```

Redis 分桶预扣 + 有界队列 + 后台批量落库，实测 1600~3000 QPS。
方案与压测方式见 `../../../docs/seckill-1000qps.md`。

## 备注

- 需要 Node.js 18+。
- Element Plus 使用全量引入，首次构建的 JS 体积较大；如果在意体积，可以改成
  `unplugin-vue-components` 按需引入。
- 库存页的压测会真实调用后端并发接口，默认 150 请求 / 并发度 6，请确认 MySQL
  `max_connections` 足够。
