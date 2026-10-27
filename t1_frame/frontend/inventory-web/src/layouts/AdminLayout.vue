<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watchEffect } from 'vue'
import { RouterLink, RouterView, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Box, Brush, Document, Menu, ShoppingBag, Tickets } from '@element-plus/icons-vue'
import { applyTheme, getInitialTheme, themes } from '@/themes'
import { setLocale, type Locale } from '@/i18n'

const route = useRoute()
const { t, locale } = useI18n()
const menuOpen = ref(false)
const now = ref(new Date())
const currentTheme = ref(getInitialTheme())
const themeOptions = themes
let timer = 0

const nav = [
  { to: '/shop', labelKey: 'nav.shop', icon: ShoppingBag },
  { to: '/inventory', labelKey: 'nav.inventory', icon: Box },
  { to: '/orders', labelKey: 'nav.orders', icon: Tickets },
  { to: '/audit', labelKey: 'nav.audit', icon: Document }
]

const pageTitle = computed(() => t((route.meta.titleKey as string) ?? 'nav.shop'))
const clock = computed(() =>
  now.value.toLocaleTimeString(locale.value === 'zh-CN' ? 'zh-CN' : 'en-US', {
    hour12: false
  })
)
const dateText = computed(() =>
  now.value.toLocaleDateString(locale.value === 'zh-CN' ? 'zh-CN' : 'en-US', {
    month: '2-digit',
    day: '2-digit',
    weekday: 'short'
  })
)

watchEffect(() => {
  document.title = `${pageTitle.value} · ${t('app.name')}`
})

function setTheme(themeId: string) {
  currentTheme.value = themeId
  applyTheme(themeId)
}

function handleThemeCommand(command: string | number | object) {
  setTheme(String(command))
}

function handleLocaleCommand(command: string | number | object) {
  const next = String(command)
  if (next === 'zh-CN' || next === 'en-US') {
    setLocale(next as Locale)
  }
}

onMounted(() => {
  timer = window.setInterval(() => {
    now.value = new Date()
  }, 1000)
})

onUnmounted(() => {
  window.clearInterval(timer)
})
</script>

<template>
  <div class="shell">
    <aside class="rail" :class="{ 'rail--open': menuOpen }">
      <div class="brand">
        <div class="brand__mark" aria-hidden="true">
          <span></span>
          <span></span>
          <span></span>
        </div>
        <div>
          <div class="brand__name">{{ t('app.name') }}</div>
          <div class="brand__sub">{{ t('app.subtitle') }}</div>
        </div>
      </div>

      <nav class="nav">
        <RouterLink
          v-for="item in nav"
          :key="item.to"
          :to="item.to"
          class="nav__item"
          @click="menuOpen = false"
        >
          <el-icon :size="16"><component :is="item.icon" /></el-icon>
          <span>{{ t(item.labelKey) }}</span>
        </RouterLink>
      </nav>

      <div class="rail__foot">
        <span class="live-dot"></span>
        <div>
          <div class="rail__foot-title">{{ t('app.localService') }}</div>
          <div class="rail__foot-sub">ABP 8.3.4 / :5288</div>
        </div>
      </div>
    </aside>

    <div v-if="menuOpen" class="mobile-scrim" @click="menuOpen = false"></div>

    <div class="main">
      <header class="topbar">
        <button class="menu-btn" aria-label="Menu" @click="menuOpen = !menuOpen">
          <el-icon :size="18"><Menu /></el-icon>
        </button>

        <div class="topbar__crumb">
          <span class="topbar__root">{{ t('app.name') }}</span>
          <span class="topbar__sep">/</span>
          <span class="topbar__page">{{ pageTitle }}</span>
        </div>

        <div class="topbar__right">
          <div class="topbar__tools">
            <el-dropdown trigger="click" @command="handleThemeCommand">
              <button class="tool-btn" type="button" :title="t('theme.label')">
                <el-icon :size="15"><Brush /></el-icon>
                <span class="tool-btn__label">{{ t('theme.label') }}</span>
              </button>
              <template #dropdown>
                <el-dropdown-menu class="theme-menu">
                  <el-dropdown-item
                    v-for="theme in themeOptions"
                    :key="theme.id"
                    :command="theme.id"
                  >
                    <span class="theme-option">
                      <span class="theme-option__swatches">
                        <i
                          v-for="(color, index) in theme.colors"
                          :key="index"
                          :style="{ background: color }"
                        ></i>
                      </span>
                      <span class="theme-option__name">{{ t(theme.nameKey) }}</span>
                      <span
                        v-if="currentTheme === theme.id"
                        class="theme-option__check"
                      >
                        ✓
                      </span>
                    </span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>

            <el-dropdown trigger="click" @command="handleLocaleCommand">
              <button
                class="tool-btn tool-btn--lang"
                type="button"
                :title="t('language.label')"
              >
                {{ locale === 'zh-CN' ? 'ZH' : 'EN' }}
              </button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="zh-CN">
                    <span :class="{ 'lang-option': true, active: locale === 'zh-CN' }">
                      {{ t('language.zh') }}
                    </span>
                  </el-dropdown-item>
                  <el-dropdown-item command="en-US">
                    <span :class="{ 'lang-option': true, active: locale === 'en-US' }">
                      {{ t('language.en') }}
                    </span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>

          <div class="clock">
            <span class="clock__time">{{ clock }}</span>
            <span class="clock__date">{{ dateText }}</span>
          </div>
          <span class="topbar__env">
            <span class="live-dot"></span>
            {{ t('app.apiConnected') }}
          </span>
        </div>
      </header>

      <main class="canvas">
        <RouterView v-slot="{ Component }">
          <Transition name="page" mode="out-in">
            <component :is="Component" />
          </Transition>
        </RouterView>
      </main>
    </div>
  </div>
</template>
