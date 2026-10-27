<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Refresh } from '@element-plus/icons-vue'
import { inventoryApi } from '@/api/inventory'
import type { AuditLog, StockFlow } from '@/types'

const { t, locale } = useI18n()
const tab = ref<'audit' | 'flow'>('audit')
const loading = ref(false)
const autoRefresh = ref(true)
const source = ref('all')

const audit = ref<AuditLog[]>([])
const auditTotal = ref(0)
const flows = ref<StockFlow[]>([])
const flowTotal = ref(0)

const auditPage = ref(1)
const flowPage = ref(1)
const pageSize = 12

let timer = 0

const sourceOptions = [
  { labelKey: 'audit.sourceAll', value: 'all' },
  { labelKey: 'source.atomic', value: 'atomic' },
  { labelKey: 'source.optimistic', value: 'optimistic' },
  { labelKey: 'source.redis', value: 'redis' },
  { labelKey: 'source.idempotent', value: 'idempotent' },
  { labelKey: 'source.sale', value: 'sale' },
  { labelKey: 'source.seckill', value: 'seckill' },
  { labelKey: 'source.cancel', value: 'cancel' },
  { labelKey: 'source.naive', value: 'naive' }
]

const knownSources = new Set(sourceOptions.map((item) => item.value))

const filteredAudit = computed(() =>
  source.value === 'all'
    ? audit.value
    : audit.value.filter((item) => item.source === source.value)
)

const filteredFlows = computed(() =>
  source.value === 'all'
    ? flows.value
    : flows.value.filter((item) => item.source === source.value)
)

const visibleCount = computed(() =>
  tab.value === 'audit' ? filteredAudit.value.length : filteredFlows.value.length
)

const pagedAudit = computed(() =>
  filteredAudit.value.slice(
    (auditPage.value - 1) * pageSize,
    auditPage.value * pageSize
  )
)

const pagedFlows = computed(() =>
  filteredFlows.value.slice(
    (flowPage.value - 1) * pageSize,
    flowPage.value * pageSize
  )
)

watch(source, () => {
  auditPage.value = 1
  flowPage.value = 1
})

function formatDate(value: string) {
  return new Date(value).toLocaleString(locale.value === 'zh-CN' ? 'zh-CN' : 'en-US', {
    hour12: false
  })
}

function sourceClass(value: string) {
  return `source-${value || 'muted'}`
}

function sourceLabel(value: string) {
  return knownSources.has(value) ? t(`source.${value}`) : value
}

async function loadAll() {
  loading.value = true
  try {
    const [auditPage, flowPage] = await Promise.all([
      inventoryApi.auditLogs(100),
      inventoryApi.stockFlows({ skip: 0, take: 200 })
    ])
    audit.value = auditPage.items
    auditTotal.value = auditPage.totalCount
    flows.value = flowPage.items
    flowTotal.value = flowPage.totalCount
  } catch {
    /* 错误消息由 axios 拦截器统一提示 */
  } finally {
    loading.value = false
  }
}

async function copyBizNo(bizNo: string) {
  try {
    await navigator.clipboard.writeText(bizNo)
    ElMessage.success(t('orders.copySuccess'))
  } catch {
    ElMessage.warning(t('common.copyUnsupported'))
  }
}

onMounted(() => {
  loadAll()
  timer = window.setInterval(() => {
    if (autoRefresh.value && !loading.value) {
      loadAll()
    }
  }, 8000)
})

onUnmounted(() => {
  window.clearInterval(timer)
})
</script>

<template>
  <section class="page">
    <header class="page-head">
      <div>
        <h1>{{ t('audit.title') }}</h1>
        <p>{{ t('audit.subtitle') }}</p>
      </div>
      <div class="page-actions">
        <el-button :icon="Refresh" :loading="loading" @click="loadAll">
          {{ t('audit.refresh') }}
        </el-button>
      </div>
    </header>

    <div class="readout-strip">
      <div class="readout">
        <div class="readout__label">{{ t('audit.messageAudit') }}</div>
        <div class="readout__value">{{ auditTotal }}</div>
        <div class="readout__hint">{{ t('audit.messageHint') }}</div>
      </div>
      <div class="readout">
        <div class="readout__label">{{ t('audit.stockFlow') }}</div>
        <div class="readout__value">{{ flowTotal }}</div>
        <div class="readout__hint">{{ t('audit.flowHint') }}</div>
      </div>
      <div class="readout">
        <div class="readout__label">{{ t('audit.currentDisplay') }}</div>
        <div class="readout__value">{{ visibleCount }}</div>
        <div class="readout__hint">{{ t('audit.displayHint') }}</div>
      </div>
      <div class="readout">
        <div class="readout__label">{{ t('audit.refreshMode') }}</div>
        <div class="readout__value" style="font-size: 18px">
          {{ autoRefresh ? t('audit.auto') : t('audit.manual') }}
        </div>
        <div class="readout__hint">{{ t('audit.autoHint') }}</div>
      </div>
    </div>

    <div class="panel">
      <div class="notice">
        <span class="notice__mark"></span>
        <span>{{ t('audit.notice') }}</span>
      </div>

      <div class="toolbar">
        <div class="segmented">
          <button :class="{ active: tab === 'audit' }" @click="tab = 'audit'">
            {{ t('audit.messageAudit') }}
          </button>
          <button :class="{ active: tab === 'flow' }" @click="tab = 'flow'">
            {{ t('audit.stockFlow') }}
          </button>
        </div>

        <el-select v-model="source" style="width: 140px">
          <el-option
            v-for="option in sourceOptions"
            :key="option.value"
            :label="t(option.labelKey)"
            :value="option.value"
          />
        </el-select>

        <div class="toolbar__spacer"></div>

        <span class="toolbar__hint">{{ t('audit.recent', { count: 100 }) }}</span>
        <el-switch v-model="autoRefresh" :active-text="t('audit.autoRefresh')" />
      </div>

      <div v-if="tab === 'audit'" class="ledger">
        <el-table v-loading="loading" :data="pagedAudit" row-key="bizNo" style="width: 100%">
          <el-table-column :label="t('audit.bizNo')" min-width="250">
            <template #default="{ row }">
              <span
                class="biz-no"
                :title="row.bizNo"
                @click="copyBizNo(row.bizNo)"
              >
                {{ row.bizNo }}
              </span>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.product')" min-width="210">
            <template #default="{ row }">
              <div class="product-cell">
                <span class="product-cell__name">{{ row.productName }}</span>
                <span class="product-cell__code">
                  SKU-{{ String(row.productId).padStart(3, '0') }}
                </span>
              </div>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.deduct')" width="90">
            <template #default="{ row }">
              <span class="num num--lg">{{ row.quantity }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.remaining')" width="90">
            <template #default="{ row }">
              <span class="num" :class="{ 'muted': row.remainingStock <= 0 }">
                {{ row.remainingStock }}
              </span>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.source')" width="130">
            <template #default="{ row }">
              <span class="stamp" :class="sourceClass(row.source)">
                {{ sourceLabel(row.source) }}
              </span>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.time')" width="210">
            <template #default="{ row }">
              <span class="mono muted">{{ formatDate(row.createdAt) }}</span>
            </template>
          </el-table-column>

          <template #empty>
            <div class="empty">
              <span class="empty__mark"></span>
              <p>{{ t('audit.noAudit') }}</p>
              <small>{{ t('audit.noAuditHint') }}</small>
            </div>
          </template>
        </el-table>
        <el-pagination
          v-model:current-page="auditPage"
          :page-size="pageSize"
          :total="filteredAudit.length"
          layout="total, prev, pager, next"
        />
      </div>

      <div v-else class="ledger">
        <el-table v-loading="loading" :data="pagedFlows" row-key="id" style="width: 100%">
          <el-table-column :label="t('audit.flowNo')" width="100">
            <template #default="{ row }">
              <span class="num">#{{ row.id }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.bizNo')" min-width="250">
            <template #default="{ row }">
              <span
                class="biz-no"
                :title="row.bizNo"
                @click="copyBizNo(row.bizNo)"
              >
                {{ row.bizNo }}
              </span>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.product')" min-width="210">
            <template #default="{ row }">
              <div class="product-cell">
                <span class="product-cell__name">{{ row.productName }}</span>
                <span class="product-cell__code">
                  SKU-{{ String(row.productId).padStart(3, '0') }}
                </span>
              </div>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.deduct')" width="90">
            <template #default="{ row }">
              <span class="num num--lg">{{ row.quantity }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.remaining')" width="90">
            <template #default="{ row }">
              <span class="num">{{ row.remainingStock }}</span>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.source')" width="130">
            <template #default="{ row }">
              <span class="stamp" :class="sourceClass(row.source)">
                {{ sourceLabel(row.source) }}
              </span>
            </template>
          </el-table-column>

          <el-table-column :label="t('audit.time')" width="210">
            <template #default="{ row }">
              <span class="mono muted">{{ formatDate(row.createdAt) }}</span>
            </template>
          </el-table-column>

          <template #empty>
            <div class="empty">
              <span class="empty__mark"></span>
              <p>{{ t('audit.noFlow') }}</p>
              <small>{{ t('audit.noFlowHint') }}</small>
            </div>
          </template>
        </el-table>
        <el-pagination
          v-model:current-page="flowPage"
          :page-size="pageSize"
          :total="filteredFlows.length"
          layout="total, prev, pager, next"
        />
      </div>
    </div>
  </section>
</template>
