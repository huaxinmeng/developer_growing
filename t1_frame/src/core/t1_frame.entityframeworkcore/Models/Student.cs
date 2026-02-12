using Abp.Domain.Entities;
using System;
using System.Collections.Generic;

namespace t1_frame.entityframeworkcore.migrations;

public partial class Student : Entity<int>
{
    // public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Sex { get; set; } = "";

    public string Sno { get; set; } = "";

    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public int CreateBy { get; set; }

    /// <summary>
    /// 花费
    /// </summary>
    public int Cost { get; set; }
}
