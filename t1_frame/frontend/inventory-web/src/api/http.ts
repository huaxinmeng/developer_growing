import axios from 'axios'
import { ElMessage } from 'element-plus'
import { localizeServerMessage } from '@/i18n/server'

const http = axios.create({
  baseURL: '/api/inventory',
  timeout: 60_000
})

http.interceptors.response.use(
  (response) => response,
  (error) => {
    const data = error?.response?.data
    const message =
      data?.error?.message ||
      data?.message ||
      error?.message

    ElMessage.error(localizeServerMessage(message))
    return Promise.reject(error)
  }
)

export default http
