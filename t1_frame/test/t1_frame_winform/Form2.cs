using DevComponents.DotNetBar.SuperGrid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace t1_frame_winform
{
    public partial class Form2 : Form
    {
        // 你的完整数据源：100 条
        private List<MyData> _allData;
        private int index = 0;
        private int minCapcity = 10;
        private int _totalCount = 0;  // 当前数据总量
        private Random rnd = new Random();
        public Form2()
        {
            InitializeComponent();

            InitGrid();
        }

        private void InitGrid()
        {
            // index = 10;
            _allData = GetData(index); // 加载 100 条数据

            var grid = superGridControl1.PrimaryGrid;

            // 1. 先加好列（设计器或代码里加都可以）
            grid.Columns.Clear();
            grid.Columns.Add(new GridColumn("Name") { Name = "colName" });
            grid.Columns.Add(new GridColumn("Age") { Name = "colAge" });

            // 2. 开启虚拟模式
            grid.VirtualMode = true;
            grid.VirtualRowCount = 0; // 告诉控件：总共 100 行
            grid.DataSource = null;                // 虚拟模式下不用 DataSource
            
            // 3. 订阅单元格数据请求事件
            // superGridControl1.PreRenderCell += SuperGridControl1_CellValueNeeded;
            superGridControl1.LoadVirtualRow += SuperGridControl1_LoadVirtualRow;
            superGridControl1.VirtualRowLoaded += SuperGridControl1_VirtualRowLoaded;
            superGridControl1.StoreVirtualRow += SuperGridControl1_StoreVirtualRow;
            superGridControl1.VScrollBar.Width = 0;
        }

        private void SuperGridControl1_StoreVirtualRow(object sender, GridVirtualRowEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void SuperGridControl1_VirtualRowLoaded(object sender, GridVirtualRowEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void SuperGridControl1_LoadVirtualRow(object sender, GridVirtualRowEventArgs e)
        {
            //throw new NotImplementedException();

            //int rowIdx = e.Index;
            //if (rowIdx < 0 || rowIdx >= _allData.Count) return;

            //var item = _allData[rowIdx];
            //richTextBoxEx1.Text += $"item {item.Name} load ! \n ";
            //// richTextBoxEx1
            //// 根据列名返回对应值
            //foreach (var cell in e.GridRow.Cells)
            //{
            //    switch (cell.GridColumn.Name)
            //    {
            //        case "colName": cell.Value = item.Name; break;
            //        case "colAge": cell.Value = item.Age; break;
            //    }
            //}

            int rowIndex = e.Index;

            // 边界检查
            if (rowIndex < 0 || rowIndex >= _allData.Count)
            {
                // 超出范围的行置空
                foreach (var cell in e.GridRow.Cells)
                {
                    cell.Value = null;
                }
                return;
            }

            var item = _allData[rowIndex];

            // 输出调试信息
            richTextBoxEx1.Text += $"加载行 {rowIndex}: {item.Name}\n";

            // 填充数据
            foreach (var cell in e.GridRow.Cells)
            {
                switch (cell.GridColumn.Name)
                {
                    case "colName":
                        cell.Value = item.Name;
                        break;
                    case "colAge":
                        cell.Value = item.Age;
                        break;
                }
            }

        }

        private void SuperGridControl1_CellValueNeeded(object sender, GridPreRenderCellEventArgs e)
        {
            int rowIdx = e.GridCell.RowIndex;
            if (rowIdx < 0 || rowIdx >= _allData.Count) return;

            var item = _allData[rowIdx];

            // 根据列名返回对应值
            switch (e.GridCell.GridColumn.Name)
            {
                case "colName": e.GridCell.Value = item.Name; break;
                case "colAge": e.GridCell.Value = item.Age; break;
            }
        }

        private List<MyData> GetData(int len)
        {
            return Enumerable.Empty<MyData>().ToList();
            //return Enumerable.Range(1, len)
            //    .Select(i => new MyData { Name = "Item " + i, Age = 20 + i % 50 })
            //    .ToList();
        }

        public class MyData
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            //var grid = superGridControl1.PrimaryGrid;
            var grid = superGridControl1.PrimaryGrid;
            var rows = grid.Rows;
            var rows1 = grid.VirtualRows;
            _totalCount++;
            var newItem = new MyData
            {
                Name = "Item " + _totalCount,
                Age = 20 + (_totalCount % 50)
            };
            _allData.Add(newItem);

            // richTextBoxEx1.Text += $"点击添加: {newItem.Name}, 总数: {_totalCount}\n";

            // 2. 关键步骤：更新 VirtualRowCount
            // var grid = superGridControl1.PrimaryGrid;
            grid.VirtualRowCount = _totalCount;

            // 3. 强制刷新控件（重新加载可见区域的数据）
            // 方法1：使用 Refresh
            //grid.SelectedRowIndex = _totalCount - 1;
            // superGridControl1.ScrollToRow =
            grid.ClearSelectedCells();
            grid.ClearSelectedRows();
            grid.SetSelectedRows(_totalCount - 1, 1, true);
            grid.GetRowFromIndex(_totalCount - 1).EnsureVisible(false);
            // superGridControl1.VScrollOffset = 
            // 4. 滚动到选中行（使其可见）
            // superGridControl1.ScrollToRow(grid.SelectedRow);
            superGridControl1.Refresh(); // 在调用 EnsureVisible 之前加上 grid.BeginUpdate() / grid.EndUpdate() 包裹操作


            // grid.VirtualRows.
            //var len = grid.Rows.Count;
            //richTextBoxEx1.tex
        }

        private void DeleteSelectedRow(int selectedIndex)
        {
            var grid = superGridControl1.PrimaryGrid;

            // 1. 获取当前选中的行索引
            // int selectedIndex = grid;
            if (selectedIndex < 0 || selectedIndex >= _allData.Count) return;

            // 2. 从真实数据源中删除数据
            _allData.RemoveAt(selectedIndex);
            _totalCount--;

            // 3. 关键步骤：重置虚拟模式，强制控件重新加载
            // 这比单纯更新 VirtualRowCount 更可靠
            grid.VirtualMode = false;
            grid.VirtualMode = true;
            grid.VirtualRowCount = _totalCount;

            // 4. 刷新控件
            superGridControl1.Refresh();

            // 5. 可选：选中删除位置的前一行（避免选中状态消失）
            if (_totalCount > 0)
            {
                int newSelectedIndex = Math.Min(selectedIndex, _totalCount - 1);
                grid.SetSelectedRows(newSelectedIndex, 1, true);
                // 滚动到新选中的行
                grid.GetRowFromIndex(newSelectedIndex).EnsureVisible(false);
            }
        }

        private void buttonX2_Click(object sender, EventArgs e)
        {
            DeleteSelectedRow(_allData.Count - 1);
        }

        private void buttonX3_Click(object sender, EventArgs e)
        {
            var rowIndex = rnd.Next(_totalCount - 1);
            // 假设修改了数据源中索引为 rowIndex 的数据
            _allData[rowIndex].Name = "新名称" + rowIndex;
            richTextBoxEx1.Text += $"重新绘制行 {rowIndex}: {"新名称" + rowIndex}\n";
            // 获取该行在 UI 上的 GridRow 对象
            var gridRow = superGridControl1.PrimaryGrid.GetRowFromIndex(rowIndex);

            // 如果该行不为 null（意味着它当前在渲染范围内），则强制刷新这行
            if (gridRow != null)
            {
                //gridRow.InvalidateRender();
                //gridRow.InvalidateLayout();
                var grid = superGridControl1.PrimaryGrid;
                //grid.VirtualMode = false;
                //grid.VirtualMode = true;
                //grid.VirtualRowCount = _totalCount;
                grid.VirtualRows.Clear(); 
                superGridControl1.Refresh();
                // superGridControl1.Refresh(rowIndex)
            }


            //// 1. 暂停布局，此时控件不会尝试重绘
            //grid.BeginUpdate();

            //try
            //{
            //    // 2. 在这里进行批量操作（如循环添加1000行）
            //    List<GridRow> rows = new List<GridRow>();
            //    for (int i = 0; i < 1000; i++)
            //    {
            //        rows.Add(new GridRow(new object[] { i, $"Item {i}" }));
            //    }
            //    grid.PrimaryGrid.Rows.AddRange(rows);

            //    // 3. 更新完成后，重新设置虚拟行数等属性
            //    grid.PrimaryGrid.VirtualRowCount = _totalCount;
            //}
            //finally
            //{
            //    // 4. 恢复布局，并一次性刷新显示最终结果
            //    grid.EndUpdate();
            //}
        }
    }
}
