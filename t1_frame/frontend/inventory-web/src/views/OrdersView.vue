<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Refresh } from '@element-plus/icons-vue'
import { inventoryApi } from '@/api/inventory'
import type { Order, SaleGroup } from '@/types'

type ViewMode = 'group' | 'flat'

const { t, locale } = useI18n()
const loading = ref(false)
const viewMode = ref<ViewMode>('group')
const status = ref<number | null>(null)

const groups = ref<SaleGroup[]>([])
const groupTotal = ref(0)
const groupPage = ref(1)

const orders = ref<Order[]>([])
const orderTotal = ref(0)
const orderPage = ref(1)

const pageSize = 12
const payingId = ref<number | null>(null)
const payingSaleNo = ref<string | null>(null)
const cancellingId = ref<number | null>(null)
const cancellingSaleNo = ref<string | null>(null)

const statusOptions: Array<{ labelKey: string; value: number | null }> = [
  { labelKey: 'common.all', value: null },
  { labelKey: 'status.pending', value: 1 },
  { labelKey: 'status.paid', value: 2 },
  { labelKey: 'status.closed', value: 5 }
]

function statusStamp(value: number) {
  if (value === 2) return { text: t('status.paid'), tone: 'ok' }
  if (value === 5) return { text: t('status.closed'), tone: 'muted' }
  return { text: t('status.pending'), tone: 'low' }
}

function formatDate(value: string) {
  return new Date(value).toLocaleString(locale.value === 'zh-CN' ? 'zh-CN' : 'en-US', {
    hour12: false
  })
}

function formatMoney(value: number) {
  return value.toLocaleString(locale.value === 'zh-CN' ? 'zh-CN' : 'en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })
}

async function loadGroups() {
  const result = await inventoryApi.saleOrders({
    skip: (groupPage.value - 1) * pageSize,
    take: pageSize,
    status: status.value
  })
  groups.value = result.items
  groupTotal.value = result.totalCount
}

async function loadOrders() {
  const result = await inventoryApi.orders({
    skip: (orderPage.value - 1) * pageSize,
    take: pageSize,
    status: status.value
  })
  orders.value = result.items
  orderTotal.value = result.totalCount
}

async function load() {
  loading.value = true
  try {
    if (viewMode.value === 'group') {
      await loadGroups()
    } else {
      await loadOrders()
    }
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    loading.value = false
  }
}

function setViewMode(mode: ViewMode) {
  if (viewMode.value === mode) return
  viewMode.value = mode
  groupPage.value = 1
  orderPage.value = 1
  load()
}

function setStatus(value: number | null) {
  if (status.value === value) return
  status.value = value
  groupPage.value = 1
  orderPage.value = 1
  load()
}

async function payGroup(group: SaleGroup) {
  try {
    await ElMessageBox.confirm(
      t('orders.paySaleConfirm', {
        saleNo: group.saleNo,
        count: group.orderCount
      }),
      t('orders.paySaleTitle'),
      {
        confirmButtonText: t('orders.confirmPay'),
        cancelButtonText: t('common.cancel'),
        type: 'warning'
      }
    )
  } catch {
    return
  }

  payingSaleNo.value = group.saleNo
  try {
    await inventoryApi.paySale(group.saleNo)
    ElMessage.success(t('orders.paySuccess'))
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    payingSaleNo.value = null
  }
}

async function cancelGroup(group: SaleGroup) {
  try {
    await ElMessageBox.confirm(
      t('orders.cancelSaleConfirm', {
        saleNo: group.saleNo,
        count: group.totalQuantity
      }),
      t('orders.cancelSaleTitle'),
      {
        confirmButtonText: t('orders.confirmCancel'),
        cancelButtonText: t('orders.back'),
        type: 'warning'
      }
    )
  } catch {
    return
  }

  cancellingSaleNo.value = group.saleNo
  try {
    await inventoryApi.cancelSale(group.saleNo)
    ElMessage.success(t('orders.cancelSuccess'))
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    cancellingSaleNo.value = null
  }
}

async function pay(order: Order) {
  payingId.value = order.id
  try {
    if (order.saleNo && order.saleNo !== order.orderNo) {
      await inventoryApi.paySale(order.saleNo)
    } else {
      await inventoryApi.payOrder(order.id)
    }
    ElMessage.success(t('orders.paySuccess'))
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    payingId.value = null
  }
}

async function cancelOrder(order: Order) {
  try {
    await ElMessageBox.confirm(
      t('orders.cancelOrderConfirm'),
      t('orders.cancelOrderTitle'),
      {
        confirmButtonText: t('orders.confirmCancel'),
        cancelButtonText: t('orders.back'),
        type: 'warning'
      }
    )
  } catch {
    return
  }

  cancellingId.value = order.id
  try {
    await inventoryApi.cancelOrder(order.id)
    ElMessage.success(t('orders.cancelSuccess'))
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    cancellingId.value = null
  }
}

async function copyOrderNo(orderNo: string) {
  try {
    await navigator.clipboard.writeText(orderNo)
    ElMessage.success(t('orders.copySuccess'))
  } catch {
    ElMessage.warning(t('common.copyUnsupported'))
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <header class="page-head">
      <div>
        <h1>{{ t('orders.title') }}</h1>
        <p>{{ t('orders.subtitle') }}</p>
      </div>
      <div class="page-actions">
        <el-button :icon="Refresh" @click="load">{{ t('orders.refresh') }}</el-button>
      </div>
    </header>

    <div class="panel" style="border-top: 1px solid var(--line); margin-top: 22px">
      <div class="toolbar">
        <div class="segmented">
          <button
            :class="{ active: viewMode === 'group' }"
            @click="setViewMode('group')"
          >
            {{ t('orders.groupMode') }}
          </button>
          <button
            :class="{ active: viewMode === 'flat' }"
            @click="setViewMode('flat')"
          >
            {{ t('orders.flatMode') }}
          </button>
        </div>

        <div class="segmented">
          <button
            v-for="option in statusOptions"
            :key="String(option.value)"
            :class="{ active: status === option.value }"
            @click="setStatus(option.value)"
          >
            {{ t(option.labelKey) }}
          </button>
        </div>

        <div class="toolbar__spacer"></div>
        <span class="toolbar__hint">
          {{
            viewMode === 'group'
              ? t('orders.totalGroups', { count: groupTotal })
              : t('orders.totalOrders', { count: orderTotal })
          }}
        </span>
      </div>

      <div v-if="viewMode === 'group'" class="ledger">
        <el-table
          v-loading="loading"
          :data="groups"
          row-key="saleNo"
          style="width: 100%"
        >
          <el-table-column type="expand">
            <template #default="{ row }">
              <div class="sale-lines">
                <div class="sale-lines__head">
                  <span>{{ t('orders.saleNo') }}</span>
                  <span>{{ t('common.product') }}</span>
                  <span>{{ t('common.quantity') }}</span>
                  <span>{{ t('common.price') }}</span>
                  <span>{{ t('common.amount') }}</span>
                  <span>{{ t('common.status') }}</span>
                </div>
                <div
                  v-for="item in row.items"
                  :key="item.id"
                  class="sale-line"
                >
                  <span class="biz-no" :title="item.orderNo">
                    {{ item.orderNo }}
                  </span>
                  <span>{{ item.productName }}</span>
                  <span class="num">× {{ item.quantity }}</span>
                  <span class="num muted">
                    {{ item.unitPrice > 0 ? `¥${formatMoney(item.unitPrice)}` : '—' }}
                  </span>
                  <span class="num">
                    {{ item.totalAmount > 0 ? `¥${formatMoney(item.totalAmount)}` : '—' }}
                  </span>
                  <span
                    class="stamp"
                    :class="`stamp--${statusStamp(item.status).tone}`"
                  >
                    {{ statusStamp(item.status).text }}
                  </span>
                </div>
              </div>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.saleNo')" min-width="230">
            <template #default="{ row }">
              <span class="biz-no" :title="row.saleNo">{{ row.saleNo }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.customer')" width="120">
            <template #default="{ row }">
              <span class="muted">{{ row.customerName || '—' }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('common.product')" width="100">
            <template #default="{ row }">
              <span class="num">{{ t('orders.lines', { count: row.orderCount }) }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.items')" width="80">
            <template #default="{ row }">
              <span class="num">{{ row.totalQuantity }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.amount')" width="120">
            <template #default="{ row }">
              <span class="num">¥{{ formatMoney(row.totalAmount) }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.status')" width="110">
            <template #default="{ row }">
              <span
                class="stamp"
                :class="`stamp--${statusStamp(row.status).tone}`"
              >
                {{ statusStamp(row.status).text }}
              </span>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.createdAt')" width="180">
            <template #default="{ row }">
              <span class="mono muted">{{ formatDate(row.createdAt) }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.actions')" width="200" align="right">
            <template #default="{ row }">
              <div class="row-actions">
                <el-button
                  v-if="row.status === 1"
                  size="small"
                  type="primary"
                  :loading="payingSaleNo === row.saleNo"
                  @click="payGroup(row)"
                >
                  {{ t('orders.paySale') }}
                </el-button>
                <el-button
                  v-if="row.status === 1 || row.status === 2"
                  size="small"
                  :loading="cancellingSaleNo === row.saleNo"
                  @click="cancelGroup(row)"
                >
                  {{ t('orders.cancelSale') }}
                </el-button>
                <span v-if="row.status === 5" class="muted">—</span>
              </div>
            </template>
          </el-table-column>

          <template #empty>
            <div class="empty">
              <span class="empty__mark"></span>
              <p>{{ t('orders.noGroups') }}</p>
              <small>{{ t('orders.noGroupsHint') }}</small>
            </div>
          </template>
        </el-table>

        <el-pagination
          v-model:current-page="groupPage"
          :page-size="pageSize"
          :total="groupTotal"
          layout="total, prev, pager, next"
          @current-change="loadGroups"
        />
      </div>

      <div v-else class="ledger">
        <el-table
          v-loading="loading"
          :data="orders"
          row-key="id"
          style="width: 100%"
        >
          <el-table-column :label="t('orders.saleNo')" min-width="250">
            <template #default="{ row }">
              <div class="product-cell">
                <span
                  class="biz-no"
                  :title="row.orderNo"
                  @click="copyOrderNo(row.orderNo)"
                >
                  {{ row.orderNo }}
                </span>
                <span class="product-cell__code">
                  {{ t('orders.saleLine', { saleNo: row.saleNo }) }}
                </span>
              </div>
            </template>
          </el-table-column>

          <el-table-column :label="t('common.product')" min-width="200">
            <template #default="{ row }">
              <div class="product-cell">
                <span class="product-cell__name">{{ row.productName }}</span>
                <span class="product-cell__code">
                  SKU-{{ String(row.productId).padStart(3, '0') }}
                </span>
              </div>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.customer')" width="120">
            <template #default="{ row }">
              <span class="muted">{{ row.customerName || '—' }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('common.quantity')" width="80">
            <template #default="{ row }">
              <span class="num num--lg">{{ row.quantity }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('common.price')" width="110">
            <template #default="{ row }">
              <span class="num muted">
                {{ row.unitPrice > 0 ? `¥${formatMoney(row.unitPrice)}` : '—' }}
              </span>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.amount')" width="120">
            <template #default="{ row }">
              <span class="num">
                {{ row.totalAmount > 0 ? `¥${formatMoney(row.totalAmount)}` : '—' }}
              </span>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.status')" width="110">
            <template #default="{ row }">
              <span
                class="stamp"
                :class="`stamp--${statusStamp(row.status).tone}`"
              >
                {{ statusStamp(row.status).text }}
              </span>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.createdAt')" width="180">
            <template #default="{ row }">
              <span class="mono muted">{{ formatDate(row.createdAt) }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('orders.actions')" width="190" align="right">
            <template #default="{ row }">
              <div class="row-actions">
                <el-button
                  v-if="row.status === 1"
                  size="small"
                  type="primary"
                  :loading="payingId === row.id"
                  @click="pay(row)"
                >
                  {{ t('orders.pay') }}
                </el-button>
                <el-button
                  v-if="row.status === 1 || row.status === 2"
                  size="small"
                  :loading="cancellingId === row.id"
                  @click="cancelOrder(row)"
                >
                  {{ t('orders.cancel') }}
                </el-button>
                <span v-if="row.status === 5" class="muted">—</span>
              </div>
            </template>
          </el-table-column>

          <template #empty>
            <div class="empty">
              <span class="empty__mark"></span>
              <p>{{ t('orders.noOrders') }}</p>
              <small>{{ t('orders.noOrdersHint') }}</small>
            </div>
          </template>
        </el-table>

        <el-pagination
          v-model:current-page="orderPage"
          :page-size="pageSize"
          :total="orderTotal"
          layout="total, prev, pager, next"
          @current-change="loadOrders"
        />
      </div>
    </div>
  </section>
</template>
