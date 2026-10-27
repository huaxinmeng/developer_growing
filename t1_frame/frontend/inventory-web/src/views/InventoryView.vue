<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh, Search, TrendCharts } from '@element-plus/icons-vue'
import { inventoryApi } from '@/api/inventory'
import { localizeServerMessage } from '@/i18n/server'
import type {
  Dashboard,
  DeductMode,
  Product,
  StressParams,
  StressResult
} from '@/types'

const { t, locale } = useI18n()
const loading = ref(false)
const products = ref<Product[]>([])
const dashboard = ref<Dashboard>({
  productCount: 0,
  totalStock: 0,
  lowStockCount: 0,
  orderCount: 0
})
const search = ref('')

const createVisible = ref(false)
const creating = ref(false)
const createForm = reactive({
  productName: '',
  stock: 100,
  price: 0,
  category: '',
  imageUrl: '',
  description: '',
  isOnSale: true
})

const adjustVisible = ref(false)
const adjusting = ref(false)
const adjustForm = reactive({ productId: 0, productName: '', stock: 0 })

const editVisible = ref(false)
const editing = ref(false)
const editProductId = ref(0)
const editProductName = ref('')
const editForm = reactive({
  price: 0,
  category: '',
  imageUrl: '',
  description: '',
  isOnSale: true
})

const deductingId = ref<number | null>(null)
const onSaleLoadingId = ref<number | null>(null)

const stressVisible = ref(false)
const stressing = ref(false)
const stressForm = reactive<StressParams>({
  mode: 'atomic',
  requests: 150,
  quantity: 1,
  initialStock: 100,
  parallelism: 6
})
const stressResult = ref<StressResult | null>(null)

const modes = computed<Array<{ value: DeductMode; label: string }>>(() => [
  { value: 'atomic', label: t('inventory.modeAtomic') },
  { value: 'optimistic', label: t('inventory.modeOptimistic') },
  { value: 'redis', label: t('inventory.modeRedis') },
  { value: 'naive', label: t('inventory.modeNaive') }
])

const modeLabels = computed<Record<string, string>>(() => ({
  atomic: t('inventory.modeAtomic'),
  optimistic: t('inventory.modeOptimistic'),
  redis: t('inventory.modeRedis'),
  naive: t('inventory.modeNaive')
}))

const modeHint = computed(() => {
  switch (stressForm.mode) {
    case 'optimistic':
      return t('inventory.hintOptimistic')
    case 'redis':
      return t('inventory.hintRedis')
    case 'naive':
      return t('inventory.hintNaive')
    default:
      return t('inventory.hintAtomic')
  }
})

function modeNote(mode: string) {
  switch (mode) {
    case 'optimistic':
      return t('inventory.noteOptimistic')
    case 'redis':
      return t('inventory.noteRedis')
    case 'naive':
      return t('inventory.noteNaive')
    default:
      return t('inventory.noteAtomic')
  }
}

const maxStock = computed(() =>
  Math.max(100, ...products.value.map((p) => p.stock), 1)
)

const filteredProducts = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return products.value
  return products.value.filter(
    (p) =>
      p.productName.toLowerCase().includes(q) ||
      String(p.id).includes(q)
  )
})

function tone(product: Product): 'ok' | 'low' | 'out' {
  if (product.stock <= 0) return 'out'
  if (product.stock <= 10) return 'low'
  return 'ok'
}

function toneText(product: Product): string {
  const value = tone(product)
  return value === 'out'
    ? t('status.soldOut')
    : value === 'low'
      ? t('status.low')
      : t('status.sufficient')
}

function filledSegments(product: Product): number {
  if (product.stock <= 0) return 0
  const ratio = product.stock / maxStock.value
  return Math.max(1, Math.min(5, Math.ceil(ratio * 5)))
}

async function load() {
  loading.value = true
  try {
    const [productPage, dash] = await Promise.all([
      inventoryApi.products(),
      inventoryApi.dashboard()
    ])
    products.value = productPage.items
    dashboard.value = dash
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    loading.value = false
  }
}

function formatMoney(value: number) {
  return value.toLocaleString(locale.value === 'zh-CN' ? 'zh-CN' : 'en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })
}

function openCreate() {
  createForm.productName = ''
  createForm.stock = 100
  createForm.price = 0
  createForm.category = ''
  createForm.imageUrl = ''
  createForm.description = ''
  createForm.isOnSale = true
  createVisible.value = true
}

async function submitCreate() {
  if (!createForm.productName.trim()) {
    ElMessage.warning(t('server.productNameRequired'))
    return
  }

  creating.value = true
  try {
    await inventoryApi.createProduct({
      productName: createForm.productName.trim(),
      stock: createForm.stock,
      price: createForm.price,
      category: createForm.category.trim() || null,
      imageUrl: createForm.imageUrl.trim() || null,
      description: createForm.description.trim() || null,
      isOnSale: createForm.isOnSale
    })
    ElMessage.success(t('inventory.created'))
    createVisible.value = false
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    creating.value = false
  }
}

function openAdjust(product: Product) {
  adjustForm.productId = product.id
  adjustForm.productName = product.productName
  adjustForm.stock = product.stock
  adjustVisible.value = true
}

async function submitAdjust() {
  adjusting.value = true
  try {
    await inventoryApi.setStock(adjustForm.productId, adjustForm.stock)
    ElMessage.success(t('inventory.stockUpdated'))
    adjustVisible.value = false
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    adjusting.value = false
  }
}

function openEdit(product: Product) {
  editProductId.value = product.id
  editProductName.value = product.productName
  editForm.price = product.price
  editForm.category = product.category ?? ''
  editForm.imageUrl = product.imageUrl ?? ''
  editForm.description = product.description ?? ''
  editForm.isOnSale = product.isOnSale
  editVisible.value = true
}

async function submitEdit() {
  editing.value = true
  try {
    await inventoryApi.updateProduct(editProductId.value, {
      price: editForm.price,
      category: editForm.category.trim() || null,
      imageUrl: editForm.imageUrl.trim() || null,
      description: editForm.description.trim() || null,
      isOnSale: editForm.isOnSale
    })
    ElMessage.success(t('inventory.updated'))
    editVisible.value = false
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    editing.value = false
  }
}

async function toggleOnSale(product: Product, value: string | number | boolean) {
  const next = Boolean(value)
  onSaleLoadingId.value = product.id
  try {
    await inventoryApi.updateProduct(product.id, {
      price: product.price,
      category: product.category,
      imageUrl: product.imageUrl,
      description: product.description,
      isOnSale: next
    })
    ElMessage.success(next ? t('inventory.onSaleOn') : t('inventory.onSaleOff'))
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    onSaleLoadingId.value = null
  }
}

async function deduct(product: Product, mode: DeductMode = 'atomic') {
  deductingId.value = product.id
  try {
    const result = await inventoryApi.deduct(mode, 1)
    if (result.success) {
      ElMessage.success(
        t('inventory.deductSuccess', {
          message: t('inventory.deductSuccessPlain'),
          stock: result.remainingStock
        })
      )
    } else {
      ElMessage.warning(
        t('inventory.deductFail', {
          message: localizeServerMessage(result.message)
        })
      )
    }
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    deductingId.value = null
  }
}

function handleMore(product: Product, command: string) {
  if (command === 'adjust') {
    openAdjust(product)
    return
  }

  deduct(product, command as DeductMode)
}

function moreHandler(product: Product) {
  return (command: string | number | object) => {
    handleMore(product, String(command))
  }
}

async function resetDemo() {
  try {
    await ElMessageBox.confirm(
      t('inventory.resetConfirm'),
      t('inventory.resetTitle'),
      {
        confirmButtonText: t('inventory.resetButton'),
        cancelButtonText: t('common.cancel'),
        type: 'warning'
      }
    )
  } catch {
    return
  }

  try {
    await inventoryApi.reset(100)
    ElMessage.success(t('inventory.resetSuccess'))
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  }
}

function openStress() {
  stressResult.value = null
  stressVisible.value = true
}

async function runStress() {
  stressing.value = true
  try {
    stressResult.value = await inventoryApi.stress({ ...stressForm })
    ElMessage.success(t('inventory.done'))
    await load()
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    stressing.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <header class="page-head">
      <div>
        <h1>{{ t('inventory.title') }}</h1>
        <p>{{ t('inventory.subtitle') }}</p>
      </div>
      <div class="page-actions">
        <el-button :icon="Refresh" @click="load">{{ t('inventory.refresh') }}</el-button>
        <el-button :icon="TrendCharts" @click="openStress">{{ t('inventory.stress') }}</el-button>
        <el-button type="primary" :icon="Plus" @click="openCreate">
          {{ t('inventory.create') }}
        </el-button>
      </div>
    </header>

    <div class="readout-strip">
      <div class="readout">
        <div class="readout__label">{{ t('inventory.totalProducts') }}</div>
        <div class="readout__value">{{ dashboard.productCount }}</div>
        <div class="readout__hint">{{ t('inventory.productCountHint') }}</div>
      </div>
      <div class="readout">
        <div class="readout__label">{{ t('inventory.totalStock') }}</div>
        <div class="readout__value">{{ dashboard.totalStock }}</div>
        <div class="readout__hint">{{ t('inventory.totalStockHint') }}</div>
      </div>
      <div class="readout" :class="{ 'readout--alert': dashboard.lowStockCount > 0 }">
        <div class="readout__label">{{ t('inventory.lowStock') }}</div>
        <div class="readout__value">{{ dashboard.lowStockCount }}</div>
        <div class="readout__hint">{{ t('inventory.lowStockHint') }}</div>
      </div>
      <div class="readout">
        <div class="readout__label">{{ t('inventory.orders') }}</div>
        <div class="readout__value">{{ dashboard.orderCount }}</div>
        <div class="readout__hint">{{ t('inventory.orderHint') }}</div>
      </div>
    </div>

    <div class="panel">
      <div class="toolbar">
        <el-input
          v-model="search"
          class="search-box"
          :prefix-icon="Search"
          :placeholder="t('inventory.searchPlaceholder')"
          clearable
        />
        <div class="toolbar__spacer"></div>
        <span class="toolbar__hint">
          {{ t('inventory.count', { filtered: filteredProducts.length, total: products.length }) }}
        </span>
        <el-button :icon="Refresh" @click="resetDemo">{{ t('inventory.reset') }}</el-button>
      </div>

      <div class="ledger">
        <el-table
          v-loading="loading"
          :data="filteredProducts"
          row-key="id"
          style="width: 100%"
        >
          <el-table-column :label="t('common.product')" min-width="240">
            <template #default="{ row }">
              <div class="product-cell">
                <span class="product-cell__name">{{ row.productName }}</span>
                <span class="product-cell__code">
                  SKU-{{ String(row.id).padStart(3, '0') }}
                </span>
              </div>
            </template>
          </el-table-column>

          <el-table-column :label="t('inventory.price')" width="130">
            <template #default="{ row }">
              <span class="num">¥{{ formatMoney(row.price) }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('inventory.currentStock')" width="170">
            <template #default="{ row }">
              <div class="stock-cell">
                <span class="num num--lg">{{ row.stock }}</span>
                <div class="stock-meter" :class="`stock-meter--${tone(row)}`">
                  <span
                    v-for="i in 5"
                    :key="i"
                    :class="{ on: i <= filledSegments(row) }"
                  ></span>
                </div>
              </div>
            </template>
          </el-table-column>

          <el-table-column :label="t('inventory.store')" width="90">
            <template #default="{ row }">
              <el-switch
                v-model="row.isOnSale"
                :loading="onSaleLoadingId === row.id"
                @change="toggleOnSale(row, $event)"
              />
            </template>
          </el-table-column>

          <el-table-column :label="t('inventory.stockStatus')" width="120">
            <template #default="{ row }">
              <span class="stamp" :class="`stamp--${tone(row)}`">
                {{ toneText(row) }}
              </span>
            </template>
          </el-table-column>

          <el-table-column :label="t('inventory.actions')" width="280" align="right">
            <template #default="{ row }">
              <div class="row-actions">
                <el-button size="small" @click="openEdit(row)">
                  {{ t('inventory.edit') }}
                </el-button>
                <el-button
                  size="small"
                  type="primary"
                  :loading="deductingId === row.id"
                  @click="deduct(row, 'atomic')"
                >
                  {{ t('inventory.deductOne') }}
                </el-button>
                <el-dropdown
                  trigger="click"
                  @command="moreHandler(row)"
                >
                  <el-button size="small">{{ t('inventory.more') }}</el-button>
                  <template #dropdown>
                    <el-dropdown-menu>
                      <el-dropdown-item command="adjust">
                        {{ t('inventory.adjustStock') }}
                      </el-dropdown-item>
                      <el-dropdown-item command="optimistic" divided>
                        {{ t('inventory.optimistic') }}
                      </el-dropdown-item>
                      <el-dropdown-item command="redis">
                        {{ t('inventory.redis') }}
                      </el-dropdown-item>
                      <el-dropdown-item command="naive" divided>
                        {{ t('inventory.naive') }}
                      </el-dropdown-item>
                    </el-dropdown-menu>
                  </template>
                </el-dropdown>
              </div>
            </template>
          </el-table-column>

          <template #empty>
            <div class="empty">
              <span class="empty__mark"></span>
              <p>{{ t('inventory.emptyTitle') }}</p>
              <small>{{ t('inventory.emptyHint') }}</small>
            </div>
          </template>
        </el-table>
      </div>
    </div>

    <el-dialog
      v-model="createVisible"
      :title="t('inventory.createTitle')"
      width="560px"
      :close-on-click-modal="false"
    >
      <div class="stress-form">
        <div class="field field--full">
          <label>{{ t('inventory.name') }}</label>
          <el-input
            v-model="createForm.productName"
            maxlength="100"
            :placeholder="t('inventory.namePlaceholder')"
          />
        </div>
        <div class="field">
          <label>{{ t('inventory.priceLabel') }}</label>
          <el-input-number
            v-model="createForm.price"
            :min="0"
            :max="999999"
            :precision="2"
            :step="10"
            controls-position="right"
            style="width: 100%"
          />
        </div>
        <div class="field">
          <label>{{ t('inventory.initialStock') }}</label>
          <el-input-number
            v-model="createForm.stock"
            :min="0"
            :max="100000"
            controls-position="right"
            style="width: 100%"
          />
        </div>
        <div class="field">
          <label>{{ t('inventory.category') }}</label>
          <el-input
            v-model="createForm.category"
            maxlength="50"
            :placeholder="t('inventory.categoryPlaceholder')"
          />
        </div>
        <div class="field">
          <label>{{ t('inventory.imageUrl') }}</label>
          <el-input v-model="createForm.imageUrl" maxlength="500" placeholder="https://..." />
        </div>
        <div class="field field--full">
          <label>{{ t('inventory.description') }}</label>
          <el-input
            v-model="createForm.description"
            type="textarea"
            :rows="3"
            maxlength="500"
            show-word-limit
            :placeholder="t('inventory.descriptionPlaceholder')"
          />
        </div>
        <div class="field field--full">
          <label>{{ t('inventory.storeStatus') }}</label>
          <el-switch v-model="createForm.isOnSale" :active-text="t('inventory.onSaleLabel')" />
        </div>
      </div>
      <template #footer>
        <el-button @click="createVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="creating" @click="submitCreate">
          {{ t('inventory.createButton') }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="editVisible"
      :title="t('inventory.editTitle')"
      width="560px"
      :close-on-click-modal="false"
    >
      <div class="stress-form">
        <div class="field field--full">
          <label>{{ t('common.product') }}</label>
          <el-input :model-value="editProductName" disabled />
        </div>
        <div class="field">
          <label>{{ t('inventory.priceLabel') }}</label>
          <el-input-number
            v-model="editForm.price"
            :min="0"
            :max="999999"
            :precision="2"
            :step="10"
            controls-position="right"
            style="width: 100%"
          />
        </div>
        <div class="field">
          <label>{{ t('inventory.category') }}</label>
          <el-input v-model="editForm.category" maxlength="50" />
        </div>
        <div class="field field--full">
          <label>{{ t('inventory.imageUrl') }}</label>
          <el-input v-model="editForm.imageUrl" maxlength="500" placeholder="https://..." />
        </div>
        <div class="field field--full">
          <label>{{ t('inventory.description') }}</label>
          <el-input
            v-model="editForm.description"
            type="textarea"
            :rows="3"
            maxlength="500"
            show-word-limit
          />
        </div>
        <div class="field field--full">
          <label>{{ t('inventory.storeStatus') }}</label>
          <el-switch v-model="editForm.isOnSale" :active-text="t('inventory.onSaleLabel')" />
        </div>
      </div>
      <template #footer>
        <el-button @click="editVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="editing" @click="submitEdit">
          {{ t('inventory.saveButton') }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="adjustVisible"
      :title="t('inventory.adjustTitle')"
      width="420px"
      :close-on-click-modal="false"
    >
      <div class="stress-form">
        <div class="field field--full">
          <label>{{ t('common.product') }}</label>
          <el-input :model-value="adjustForm.productName" disabled />
        </div>
        <div class="field field--full">
          <label>{{ t('inventory.newStock') }}</label>
          <el-input-number
            v-model="adjustForm.stock"
            :min="0"
            :max="100000"
            controls-position="right"
            style="width: 100%"
          />
          <div class="field__hint">{{ t('inventory.adjustHint') }}</div>
        </div>
      </div>
      <template #footer>
        <el-button @click="adjustVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="adjusting" @click="submitAdjust">
          {{ t('inventory.saveStock') }}
        </el-button>
      </template>
    </el-dialog>

    <el-drawer
      v-model="stressVisible"
      :title="t('inventory.stressTitle')"
      size="440px"
      :close-on-click-modal="false"
    >
      <div class="stress-form">
        <div class="field field--full">
          <label>{{ t('inventory.mode') }}</label>
          <div class="segmented">
            <button
              v-for="mode in modes"
              :key="mode.value"
              :class="{ active: stressForm.mode === mode.value }"
              @click="stressForm.mode = mode.value"
            >
              {{ mode.label }}
            </button>
          </div>
          <div class="field__hint">{{ modeHint }}</div>
        </div>

        <div class="field">
          <label>{{ t('inventory.requests') }}</label>
          <el-input-number
            v-model="stressForm.requests"
            :min="1"
            :max="300"
            controls-position="right"
            style="width: 100%"
          />
        </div>
        <div class="field">
          <label>{{ t('inventory.quantity') }}</label>
          <el-input-number
            v-model="stressForm.quantity"
            :min="1"
            :max="100"
            controls-position="right"
            style="width: 100%"
          />
        </div>
        <div class="field">
          <label>{{ t('inventory.initialStock') }}</label>
          <el-input-number
            v-model="stressForm.initialStock"
            :min="1"
            :max="100000"
            controls-position="right"
            style="width: 100%"
          />
        </div>
        <div class="field">
          <label>{{ t('inventory.parallelism') }}</label>
          <el-input-number
            v-model="stressForm.parallelism"
            :min="1"
            :max="16"
            controls-position="right"
            style="width: 100%"
          />
          <div class="field__hint">{{ t('inventory.parallelismHint') }}</div>
        </div>
      </div>

      <el-button
        type="primary"
        style="width: 100%; margin-top: 20px"
        :loading="stressing"
        @click="runStress"
      >
        {{ t('inventory.run') }}
      </el-button>

      <div v-if="stressResult" class="stress-result">
        <div class="stress-result__head">
          <strong>{{ modeLabels[stressResult.mode] ?? stressResult.mode }}</strong>
          <span>{{ stressResult.elapsedMs }} ms</span>
        </div>
        <div class="stress-grid">
          <div class="stress-stat">
            <div class="stress-stat__label">{{ t('inventory.success') }}</div>
            <div class="stress-stat__value">{{ stressResult.successCount }}</div>
          </div>
          <div class="stress-stat">
            <div class="stress-stat__label">{{ t('inventory.failed') }}</div>
            <div class="stress-stat__value">{{ stressResult.failCount }}</div>
          </div>
          <div class="stress-stat">
            <div class="stress-stat__label">{{ t('inventory.finalStock') }}</div>
            <div class="stress-stat__value">{{ stressResult.finalStock }}</div>
          </div>
          <div class="stress-stat">
            <div class="stress-stat__label">{{ t('inventory.deducted') }}</div>
            <div class="stress-stat__value">
              {{ stressResult.deductedQuantity }}
            </div>
          </div>
          <div class="stress-stat">
            <div class="stress-stat__label">{{ t('inventory.lostUpdates') }}</div>
            <div
              class="stress-stat__value"
              :class="{ 'is-alert': stressResult.lostUpdates > 0 }"
            >
              {{ stressResult.lostUpdates }}
            </div>
          </div>
          <div class="stress-stat">
            <div class="stress-stat__label">{{ t('inventory.parallelism') }}</div>
            <div class="stress-stat__value">{{ stressResult.parallelism }}</div>
          </div>
        </div>
        <p class="stress-note">{{ modeNote(stressResult.mode) }}</p>
      </div>
    </el-drawer>
  </section>
</template>
