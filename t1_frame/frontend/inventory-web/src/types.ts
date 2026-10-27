export interface PagedResult<T> {
  totalCount: number
  items: T[]
}

export interface Product {
  id: number
  productName: string
  stock: number
  price: number
  description: string | null
  imageUrl: string | null
  category: string | null
  isOnSale: boolean
}

export interface Dashboard {
  productCount: number
  totalStock: number
  lowStockCount: number
  orderCount: number
}

export interface Order {
  id: number
  orderNo: string
  saleNo: string
  productId: number
  productName: string
  quantity: number
  unitPrice: number
  totalAmount: number
  customerName: string | null
  status: number
  createdAt: string
}

export interface SaleGroup {
  saleNo: string
  customerName: string | null
  status: number
  totalAmount: number
  totalQuantity: number
  orderCount: number
  createdAt: string
  items: Order[]
}

export interface StockFlow {
  id: number
  bizNo: string
  productId: number
  productName: string
  quantity: number
  remainingStock: number
  source: string
  createdAt: string
}

export interface AuditLog {
  bizNo: string
  productId: number
  productName: string
  quantity: number
  remainingStock: number
  source: string
  createdAt: string
}

export interface CreateProductInput {
  productName: string
  stock: number
  price: number
  description?: string | null
  imageUrl?: string | null
  category?: string | null
  isOnSale: boolean
}

export interface UpdateProductInput {
  price: number
  description?: string | null
  imageUrl?: string | null
  category?: string | null
  isOnSale: boolean
}

export interface SaleItemInput {
  productId: number
  quantity: number
}

export interface CreateSaleInput {
  requestId: string
  customerName?: string
  items: SaleItemInput[]
}

export interface SaleItemResult {
  productId: number
  orderNo: string
  productName: string
  quantity: number
  unitPrice: number
  totalAmount: number
  remainingStock: number
}

export interface SaleResult {
  success: boolean
  saleNo: string
  message: string
  totalAmount: number
  items: SaleItemResult[]
}

export interface DeductResult {
  success: boolean
  message: string
  remainingStock: number
  attempts: number
  bizNo: string
  source: string
  eventPublished: boolean
}

export type DeductMode = 'naive' | 'atomic' | 'optimistic' | 'redis'

export interface StressParams {
  mode: DeductMode
  requests: number
  quantity: number
  initialStock: number
  parallelism: number
}

export interface StressResult {
  mode: string
  requests: number
  parallelism: number
  quantity: number
  initialStock: number
  finalStock: number
  successCount: number
  failCount: number
  deductedQuantity: number
  lostUpdates: number
  oversold: boolean
  elapsedMs: number
  note: string
}
