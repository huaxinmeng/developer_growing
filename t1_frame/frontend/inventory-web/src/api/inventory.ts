import http from './http'
import type {
  AuditLog,
  CreateProductInput,
  CreateSaleInput,
  Dashboard,
  DeductMode,
  DeductResult,
  Order,
  PagedResult,
  Product,
  SaleGroup,
  SaleResult,
  StockFlow,
  StressParams,
  StressResult,
  UpdateProductInput
} from '@/types'

export const inventoryApi = {
  dashboard() {
    return http.get<Dashboard>('/dashboard').then((r) => r.data)
  },

  products() {
    return http.get<PagedResult<Product>>('/products').then((r) => r.data)
  },

  createProduct(input: CreateProductInput) {
    return http.post<Product>('/products', input).then((r) => r.data)
  },

  setStock(productId: number, stock: number) {
    return http
      .post<Product>(`/products/${productId}/stock`, null, { params: { stock } })
      .then((r) => r.data)
  },

  updateProduct(productId: number, input: UpdateProductInput) {
    return http
      .put<Product>(`/products/${productId}`, input)
      .then((r) => r.data)
  },

  createSale(input: CreateSaleInput) {
    return http.post<SaleResult>('/sales', input).then((r) => r.data)
  },

  payOrder(orderId: number) {
    return http
      .post<Order>(`/orders/${orderId}/pay`)
      .then((r) => r.data)
  },

  cancelOrder(orderId: number) {
    return http
      .post<SaleGroup>(`/orders/${orderId}/cancel`)
      .then((r) => r.data)
  },

  saleOrders(params: { skip: number; take: number; status?: number | null }) {
    return http
      .get<PagedResult<SaleGroup>>('/sale-orders', { params })
      .then((r) => r.data)
  },

  paySale(saleNo: string) {
    return http
      .post<SaleGroup>(`/sale-orders/${encodeURIComponent(saleNo)}/pay`)
      .then((r) => r.data)
  },

  cancelSale(saleNo: string) {
    return http
      .post<SaleGroup>(`/sale-orders/${encodeURIComponent(saleNo)}/cancel`)
      .then((r) => r.data)
  },

  orders(params: { skip: number; take: number; status?: number | null }) {
    return http
      .get<PagedResult<Order>>('/orders', { params })
      .then((r) => r.data)
  },

  stockFlows(params: { skip: number; take: number }) {
    return http
      .get<PagedResult<StockFlow>>('/stock-flows', { params })
      .then((r) => r.data)
  },

  auditLogs(take = 100) {
    return http
      .get<PagedResult<AuditLog>>('/audit-logs', { params: { take } })
      .then((r) => r.data)
  },

  reset(stock = 100) {
    return http
      .post<Product>('/reset', null, { params: { stock } })
      .then((r) => r.data)
  },

  deduct(mode: DeductMode, quantity = 1) {
    return http
      .post<DeductResult>(`/deduct/${mode}`, null, { params: { quantity } })
      .then((r) => r.data)
  },

  deductIdempotent(bizNo: string, quantity = 1) {
    return http
      .post<DeductResult>('/deduct/idempotent', null, { params: { bizNo, quantity } })
      .then((r) => r.data)
  },

  stress(params: StressParams) {
    return http
      .post<StressResult>('/stress', null, { params })
      .then((r) => r.data)
  }
}
