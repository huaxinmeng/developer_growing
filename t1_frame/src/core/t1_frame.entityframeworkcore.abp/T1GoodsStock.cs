using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;

namespace t1_frame.entityframeworkcore.abp
{
    [Table("t1_goods_stock")]
    public class T1GoodsStock : Entity<long>
    {
        /// <summary>
        /// 商品名称
        /// </summary>
        [MaxLength(100)]
        public string goods_name {  get; set; }

        /// <summary>
        /// 商品库存
        /// </summary>
        public int goods_stock {  get; set; }
    }
}
