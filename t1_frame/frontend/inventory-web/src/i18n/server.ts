import { translate } from './index'

interface MessageRule {
  pattern: RegExp
  key: string
  map?: (match: RegExpMatchArray) => Record<string, unknown>
}

const rules: MessageRule[] = [
  {
    pattern: /商品「(.+?)」库存不足/,
    key: 'server.insufficientForProduct',
    map: (match) => ({ name: match[1] })
  },
  { pattern: /库存不足/, key: 'server.insufficientStock' },
  {
    pattern: /商品「(.+?)」已下架/,
    key: 'server.offSaleForProduct',
    map: (match) => ({ name: match[1] })
  },
  { pattern: /已下架/, key: 'server.offSale' },
  { pattern: /商品已删除/, key: 'server.productDeleted' },
  {
    pattern: /商品 (\d+) 不存在/,
    key: 'server.productIdNotFound',
    map: (match) => ({ id: match[1] })
  },
  { pattern: /商品不存在/, key: 'server.productNotFound' },
  { pattern: /重复请求/, key: 'server.duplicate' },
  { pattern: /销售单不存在/, key: 'server.saleNotFound' },
  { pattern: /已取消的销售单不能支付/, key: 'server.cancelledCannotPay' },
  { pattern: /已关闭的订单不能支付/, key: 'server.closedCannotPay' },
  { pattern: /订单不存在/, key: 'server.orderNotFound' },
  { pattern: /订单状态已变化/, key: 'server.statusChanged' },
  { pattern: /购买数量必须大于 0/, key: 'server.quantityPositive' },
  { pattern: /请选择商品/, key: 'server.selectProduct' },
  { pattern: /商品名称不能为空/, key: 'server.productNameRequired' },
  { pattern: /库存不能为负数/, key: 'server.stockNegative' },
  { pattern: /价格不能为负数/, key: 'server.priceNegative' },
  {
    pattern: /商品「(.+?)」已存在/,
    key: 'server.productExists',
    map: (match) => ({ name: match[1] })
  }
]

export function localizeServerMessage(message: string | null | undefined): string {
  if (!message) {
    return translate('server.unknown')
  }

  for (const rule of rules) {
    const match = message.match(rule.pattern)
    if (match) {
      return translate(rule.key, rule.map?.(match))
    }
  }

  return message
}
