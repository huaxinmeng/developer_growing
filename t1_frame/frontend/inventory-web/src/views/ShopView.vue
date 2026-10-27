<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Refresh, Search, ShoppingCart } from '@element-plus/icons-vue'
import { inventoryApi } from '@/api/inventory'
import { localizeServerMessage } from '@/i18n/server'
import type { Product, SaleResult } from '@/types'

interface CartLine {
  productId: number
  quantity: number
}

const CART_KEY = 'inventory-cart-lines'

const router = useRouter()
const { t, locale } = useI18n()
const loading = ref(false)
const products = ref<Product[]>([])
const search = ref('')
const category = ref('__all__')

const detailVisible = ref(false)
const detailProduct = ref<Product | null>(null)
const detailQuantity = ref(1)

const cartVisible = ref(false)
const cart = ref<CartLine[]>([])
const customerName = ref('')
const submitting = ref(false)
const saleResult = ref<SaleResult | null>(null)

const shopProducts = computed(() => products.value.filter((p) => p.isOnSale))

const categoryKeys: Record<string, string> = {
  手机: 'category.phone',
  外设: 'category.peripherals',
  音频: 'category.audio',
  显示: 'category.display'
}

function categoryLabel(value: string) {
  const key = categoryKeys[value]
  return key ? t(key) : value
}

const categories = computed(() => {
  const values = shopProducts.value
    .map((p) => p.category)
    .filter((value): value is string => Boolean(value))

  return ['__all__', ...new Set(values)]
})

const filteredProducts = computed(() => {
  const q = search.value.trim().toLowerCase()

  return shopProducts.value.filter((product) => {
    const matchesCategory =
      category.value === '__all__' || product.category === category.value
    const matchesSearch =
      !q ||
      product.productName.toLowerCase().includes(q) ||
      (product.description ?? '').toLowerCase().includes(q)

    return matchesCategory && matchesSearch
  })
})

const cartItems = computed(() => {
  const result: Array<{ product: Product; quantity: number }> = []

  for (const line of cart.value) {
    const product = products.value.find((p) => p.id === line.productId)
    if (product) {
      result.push({ product, quantity: line.quantity })
    }
  }

  return result
})

const cartCount = computed(() =>
  cart.value.reduce((sum, line) => sum + line.quantity, 0)
)

const cartTotal = computed(() =>
  cartItems.value.reduce(
    (sum, item) => sum + item.product.price * item.quantity,
    0
  )
)

function formatMoney(value: number) {
  return value.toLocaleString(locale.value === 'zh-CN' ? 'zh-CN' : 'en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })
}

async function load() {
  loading.value = true
  try {
    const result = await inventoryApi.products()
    products.value = result.items
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    loading.value = false
  }
}

function openDetail(product: Product) {
  detailProduct.value = product
  detailQuantity.value = 1
  detailVisible.value = true
}

function addToCart(product: Product, quantity = 1) {
  if (product.stock <= 0) {
    ElMessage.warning(t('shop.soldOutToast'))
    return
  }

  const line = cart.value.find((item) => item.productId === product.id)
  if (line) {
    const next = Math.min(product.stock, line.quantity + quantity)
    if (next === line.quantity) {
      ElMessage.warning(t('shop.maxStock'))
      return
    }
    line.quantity = next
  } else {
    cart.value.push({
      productId: product.id,
      quantity: Math.min(product.stock, quantity)
    })
  }

  saleResult.value = null
  ElMessage.success(t('shop.added'))
}

function addFromDetail() {
  if (!detailProduct.value) return
  addToCart(detailProduct.value, detailQuantity.value)
  detailVisible.value = false
}

function buyNow(product: Product) {
  if (product.stock <= 0) return

  cart.value = [
    {
      productId: product.id,
      quantity: Math.min(product.stock, detailQuantity.value)
    }
  ]
  detailVisible.value = false
  saleResult.value = null
  cartVisible.value = true
}

function openCart() {
  saleResult.value = null
  cartVisible.value = true
}

function setQuantity(productId: number, value: number) {
  const line = cart.value.find((item) => item.productId === productId)
  const product = products.value.find((item) => item.id === productId)
  if (!line || !product) return

  line.quantity = Math.max(1, Math.min(product.stock, Math.floor(value) || 1))
}

function quantityHandler(productId: number) {
  return (value: number | undefined) => setQuantity(productId, value ?? 1)
}

function removeLine(productId: number) {
  cart.value = cart.value.filter((item) => item.productId !== productId)
}

async function checkout() {
  if (!cartItems.value.length) {
    ElMessage.warning(t('shop.emptyCartTitle'))
    return
  }

  submitting.value = true
  try {
    const requestId = `SALE${Date.now()}${Math.floor(Math.random() * 1000)}`

    const result = await inventoryApi.createSale({
      requestId,
      customerName: customerName.value.trim() || undefined,
      items: cartItems.value.map((item) => ({
        productId: item.product.id,
        quantity: item.quantity
      }))
    })

    if (result.success) {
      saleResult.value = result
      cart.value = []
      ElMessage.success(t('shop.orderSuccess'))
      await load()
    } else {
      ElMessage.warning(localizeServerMessage(result.message))
      await load()
    }
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    submitting.value = false
  }
}

function continueShopping() {
  saleResult.value = null
  cartVisible.value = false
  load()
}

function goOrders() {
  cartVisible.value = false
  router.push('/orders')
}

onMounted(() => {
  load()

  try {
    const raw = localStorage.getItem(CART_KEY)
    if (!raw) return
    const parsed = JSON.parse(raw)
    if (Array.isArray(parsed)) {
      cart.value = parsed
        .filter(
          (item) =>
            typeof item?.productId === 'number' &&
            typeof item?.quantity === 'number'
        )
        .map((item) => ({
          productId: item.productId,
          quantity: Math.max(1, item.quantity)
        }))
    }
  } catch {
    localStorage.removeItem(CART_KEY)
  }
})

watch(
  cart,
  (value) => {
    localStorage.setItem(CART_KEY, JSON.stringify(value))
  },
  { deep: true }
)
</script>

<template>
  <section class="page">
    <header class="page-head">
      <div>
        <h1>{{ t('shop.title') }}</h1>
        <p>{{ t('shop.subtitle') }}</p>
      </div>
      <div class="page-actions">
        <el-button :icon="Refresh" @click="load">{{ t('shop.refresh') }}</el-button>
        <el-button type="primary" :icon="ShoppingCart" @click="openCart">
          {{ t('shop.cart') }}
          <span v-if="cartCount" class="cart-count">{{ cartCount }}</span>
        </el-button>
      </div>
    </header>

    <div class="shop-note">
      <span class="notice__mark"></span>
      <span>{{ t('shop.note') }}</span>
    </div>

    <div class="shop-toolbar">
      <el-input
        v-model="search"
        class="search-box"
        :prefix-icon="Search"
        :placeholder="t('shop.searchPlaceholder')"
        clearable
      />

      <div class="category-row">
        <button
          v-for="item in categories"
          :key="item"
          :class="{ active: category === item }"
          @click="category = item"
        >
          {{ item === '__all__' ? t('common.all') : categoryLabel(item) }}
        </button>
      </div>

      <div class="toolbar__spacer"></div>
      <span class="toolbar__hint">
        {{ t('shop.inSale', { count: filteredProducts.length }) }}
      </span>
    </div>

    <div v-if="filteredProducts.length" class="shop-grid" v-loading="loading">
      <article
        v-for="product in filteredProducts"
        :key="product.id"
        class="shop-card"
      >
        <button
          type="button"
          class="shop-card__media"
          :aria-label="`${t('common.view')} ${product.productName}`"
          @click="openDetail(product)"
        >
          <el-image
            :src="product.imageUrl || ''"
            fit="cover"
            lazy
            style="width: 100%; height: 100%"
          >
            <template #error>
              <div class="shop-card__fallback">
                {{ product.productName.slice(0, 1) }}
              </div>
            </template>
            <template #placeholder>
              <div class="shop-card__fallback">
                {{ product.productName.slice(0, 1) }}
              </div>
            </template>
          </el-image>
          <span v-if="product.stock <= 0" class="shop-card__soldout">
            {{ t('shop.soldOut') }}
          </span>
        </button>

        <div class="shop-card__body">
          <span class="shop-card__category">
            {{ product.category ? categoryLabel(product.category) : t('shop.uncategorized') }}
          </span>
          <h3>{{ product.productName }}</h3>
          <p class="shop-card__desc">
            {{ product.description || t('shop.noDescription') }}
          </p>

          <div class="shop-card__meta">
            <span class="shop-price">¥{{ formatMoney(product.price) }}</span>
            <span
              class="shop-stock"
              :class="{ 'is-low': product.stock > 0 && product.stock <= 10 }"
            >
              {{
                product.stock > 0
                  ? t('shop.stockCount', { count: product.stock })
                  : t('shop.soldOut')
              }}
            </span>
          </div>

          <div class="shop-card__actions">
            <el-button @click="openDetail(product)">{{ t('common.view') }}</el-button>
            <el-button
              type="primary"
              :disabled="product.stock <= 0"
              @click="addToCart(product)"
            >
              {{ t('shop.addToCart') }}
            </el-button>
          </div>
        </div>
      </article>
    </div>

    <div v-else class="empty" style="padding: 70px 20px">
      <span class="empty__mark"></span>
      <p>{{ t('shop.emptyTitle') }}</p>
      <small>{{ t('shop.emptyHint') }}</small>
    </div>

    <el-dialog
      v-model="detailVisible"
      :title="t('shop.detailTitle')"
      width="680px"
      :close-on-click-modal="false"
    >
      <div v-if="detailProduct" class="shop-detail">
        <div class="shop-detail__media">
          <el-image
            :src="detailProduct.imageUrl || ''"
            fit="cover"
            style="width: 100%; height: 100%"
          >
            <template #error>
              <div class="shop-card__fallback">
                {{ detailProduct.productName.slice(0, 1) }}
              </div>
            </template>
          </el-image>
        </div>

        <div class="shop-detail__body">
          <span class="shop-card__category">
            {{ detailProduct.category ? categoryLabel(detailProduct.category) : t('shop.uncategorized') }}
          </span>
          <h2>{{ detailProduct.productName }}</h2>
          <p>{{ detailProduct.description || t('shop.noDescription') }}</p>

          <div class="shop-price shop-price--lg">
            ¥{{ formatMoney(detailProduct.price) }}
          </div>
          <div class="shop-detail__stock">
            {{ t('shop.currentStock', { count: detailProduct.stock }) }}
          </div>

          <div class="field">
            <label>{{ t('shop.buyQuantity') }}</label>
            <el-input-number
              v-model="detailQuantity"
              :min="1"
              :max="Math.max(1, detailProduct.stock)"
              controls-position="right"
            />
          </div>

          <div class="shop-detail__actions">
            <el-button :disabled="detailProduct.stock <= 0" @click="addFromDetail">
              {{ t('shop.addToCart') }}
            </el-button>
            <el-button
              type="primary"
              :disabled="detailProduct.stock <= 0"
              @click="buyNow(detailProduct)"
            >
              {{ t('shop.buyNow') }}
            </el-button>
          </div>
        </div>
      </div>
    </el-dialog>

    <el-drawer
      v-model="cartVisible"
      :title="t('shop.cartTitle')"
      size="440px"
      :close-on-click-modal="false"
    >
      <div v-if="saleResult" class="sale-success">
        <div class="sale-success__mark">✓</div>
        <h2>{{ t('shop.successTitle') }}</h2>
        <p>
          {{ t('shop.saleNo') }}
          <span class="mono">{{ saleResult.saleNo }}</span>
        </p>

        <div class="sale-success__items">
          <div
            v-for="item in saleResult.items"
            :key="item.orderNo"
            class="sale-success__item"
          >
            <span>{{ item.productName }}</span>
            <span class="mono">× {{ item.quantity }}</span>
            <span class="mono">¥{{ formatMoney(item.totalAmount) }}</span>
          </div>
        </div>

        <div class="sale-success__total">
          <span>{{ t('shop.total') }}</span>
          <strong>¥{{ formatMoney(saleResult.totalAmount) }}</strong>
        </div>

        <el-button type="primary" style="width: 100%" @click="goOrders">
          {{ t('shop.goOrders') }}
        </el-button>
        <el-button text style="width: 100%" @click="continueShopping">
          {{ t('shop.continueShopping') }}
        </el-button>
      </div>

      <div v-else-if="cartItems.length" class="cart-body">
        <div
          v-for="item in cartItems"
          :key="item.product.id"
          class="cart-line"
        >
          <div class="cart-line__thumb">
            <el-image
              :src="item.product.imageUrl || ''"
              fit="cover"
              style="width: 100%; height: 100%"
            >
              <template #error>
                <div class="shop-card__fallback">
                  {{ item.product.productName.slice(0, 1) }}
                </div>
              </template>
            </el-image>
          </div>

          <div class="cart-line__info">
            <div class="cart-line__name">{{ item.product.productName }}</div>
            <div class="cart-line__price mono">
              ¥{{ formatMoney(item.product.price) }}
            </div>
            <div class="cart-line__controls">
              <el-input-number
                :model-value="item.quantity"
                :min="1"
                :max="item.product.stock"
                size="small"
                style="width: 100px"
                @change="quantityHandler(item.product.id)"
              />
              <el-button text @click="removeLine(item.product.id)">
                {{ t('shop.remove') }}
              </el-button>
            </div>
          </div>

          <div class="cart-line__total mono">
            ¥{{ formatMoney(item.product.price * item.quantity) }}
          </div>
        </div>

        <div class="cart-form">
          <div class="field">
            <label>{{ t('shop.customer') }}</label>
            <el-input
              v-model="customerName"
              maxlength="100"
              :placeholder="t('shop.customerPlaceholder')"
            />
          </div>
        </div>

        <div class="cart-summary">
          <span>{{ t('shop.total') }}</span>
          <strong>¥{{ formatMoney(cartTotal) }}</strong>
        </div>

        <el-button
          type="primary"
          style="width: 100%"
          :loading="submitting"
          @click="checkout"
        >
          {{ t('shop.submit') }}
        </el-button>
        <p class="cart-hint">{{ t('shop.submitHint') }}</p>
      </div>

      <div v-else class="empty" style="padding: 60px 20px">
        <span class="empty__mark"></span>
        <p>{{ t('shop.emptyCartTitle') }}</p>
        <small>{{ t('shop.emptyCartHint') }}</small>
      </div>
    </el-drawer>
  </section>
</template>
