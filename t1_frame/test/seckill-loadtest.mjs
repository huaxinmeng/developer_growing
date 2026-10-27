import http from 'node:http'
import { performance } from 'node:perf_hooks'

const base = process.argv[2] ?? 'http://localhost:5288'
const total = Number(process.argv[3] ?? 5000)
const concurrency = Number(process.argv[4] ?? 200)
const productId = Number(process.argv[5] ?? 2)

const target = new URL(base)
const agent = new http.Agent({
  keepAlive: true,
  maxSockets: concurrency,
  maxFreeSockets: concurrency
})

function request(method, path) {
  return new Promise((resolve, reject) => {
    const req = http.request(
      {
        hostname: target.hostname,
        port: target.port || 80,
        path,
        method,
        agent,
        headers: {
          'Content-Length': 0,
          Connection: 'keep-alive'
        }
      },
      (res) => {
        let body = ''
        res.setEncoding('utf8')
        res.on('data', (chunk) => (body += chunk))
        res.on('end', () => {
          if (res.statusCode >= 200 && res.statusCode < 300) {
            try {
              resolve(body ? JSON.parse(body) : null)
            } catch (error) {
              reject(new Error(`invalid json: ${body.slice(0, 200)}`))
            }
          } else {
            reject(new Error(`HTTP ${res.statusCode}: ${body.slice(0, 300)}`))
          }
        })
      }
    )

    req.on('error', reject)
    req.end()
  })
}

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms))
const runTag = Date.now().toString(36)
const stock = total + 1000

console.log(`reset: product=${productId}, stock=${stock}`)
await request(
  'POST',
  `/api/seckill/reset?productId=${productId}&stock=${stock}`
)

let cursor = 0
let accepted = 0
let rejected = 0
let errors = 0

const started = performance.now()

const workers = Array.from({ length: concurrency }, async () => {
  while (true) {
    const index = cursor++
    if (index >= total) {
      return
    }

    const requestId = `LT-${runTag}-${index}`

    try {
      const result = await request(
        'POST',
        `/api/seckill/${productId}?quantity=1&requestId=${encodeURIComponent(requestId)}`
      )

      if (result?.accepted) {
        accepted += 1
      } else {
        rejected += 1
      }
    } catch {
      errors += 1
    }
  }
})

await Promise.all(workers)
const elapsed = (performance.now() - started) / 1000

console.log(
  `load: total=${total}, concurrency=${concurrency}, accepted=${accepted}, rejected=${rejected}, errors=${errors}, elapsed=${elapsed.toFixed(2)}s, qps=${(accepted / elapsed).toFixed(1)}`
)

const deadline = Date.now() + 60_000
let stats = null

while (Date.now() < deadline) {
  stats = await request('GET', `/api/seckill/stats?productId=${productId}`)

  if (stats.created + stats.failed >= stats.accepted) {
    break
  }

  await sleep(500)
}

console.log(
  `stats: accepted=${stats?.accepted ?? '?'}, created=${stats?.created ?? '?'}, failed=${stats?.failed ?? '?'}, pending=${stats?.pending ?? '?'}, remaining=${stats?.totalStock ?? '?'}, buckets=${stats?.buckets?.map((x) => x.stock).join('/') ?? '?'}`
)

if (stats && stats.created + stats.failed < stats.accepted) {
  console.error('worker did not drain the queue within 60s')
  process.exitCode = 1
}

agent.destroy()
