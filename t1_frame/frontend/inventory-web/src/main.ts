import { createApp } from 'vue'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import './styles/main.css'
import App from './App.vue'
import router from './router'
import { i18n } from './i18n'
import { applyTheme, getInitialTheme } from './themes'

applyTheme(getInitialTheme())

createApp(App)
  .use(router)
  .use(i18n)
  .use(ElementPlus)
  .mount('#app')
