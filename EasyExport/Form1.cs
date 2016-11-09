using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyExport
{
    public partial class Form1 : Form
    {
        private static int ColumnDefaultY = 10;
        private static int ColumnY = ColumnDefaultY;
        private static Dictionary<string, Common.Excel.ColumnDataType> ColumnDataTypes = new Dictionary<string, Common.Excel.ColumnDataType>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ColumnDataTypes.Add("普通文本", Common.Excel.ColumnDataType.String);
            ColumnDataTypes.Add("日期", Common.Excel.ColumnDataType.Date);
            ColumnDataTypes.Add("日期+时间", Common.Excel.ColumnDataType.DateTime);
            ColumnDataTypes.Add("真假", Common.Excel.ColumnDataType.Bool);
            ColumnDataTypes.Add("整型", Common.Excel.ColumnDataType.Int);
            ColumnDataTypes.Add("浮点型", Common.Excel.ColumnDataType.Float);
            ColumnDataTypes.Add("超链接", Common.Excel.ColumnDataType.Url);
            ColumnDataTypes.Add("其它", Common.Excel.ColumnDataType.Other);
        }

        private void btnTestConn_Click(object sender, EventArgs e)
        {
            try
            {
                string sql = @"Select   name   from   master..sysdatabases where   name   not   in('master','model','msdb','tempdb','northwind','pubs')";
                var dt = Query(GetConnetionString(), sql);
                if (dt != null && dt.Rows.Count > 0)
                {
                    this.cmbDatabase.Enabled = true;
                    this.cmbDatabase.Items.Clear();
                    this.cmbDatabase.DataSource = dt.AsEnumerable().Select(r => r["name"].ToString()).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTestSql_Click(object sender, EventArgs e)
        {
            string sql = this.txtSql.Text;

            try
            {
                var dt = Query(GetConnetionString(this.cmbDatabase.SelectedItem.ToString()), sql);

                AddGroupTitleSetControl(dt.Columns.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            string sql = this.txtSql.Text;

            try
            {
                var dt = Query(GetConnetionString(this.cmbDatabase.SelectedItem.ToString()), sql);

                var columns = new List<Common.Excel.Column>();
                var list = GetGroupTitleSet(dt.Columns.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    columns.Add(new Common.Excel.Column(dt.Columns[i].ColumnName, list[i].Title, ColumnDataTypes[list[i].Type], list[i].Width));
                }

                var fileDialog = new SaveFileDialog();
                fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                fileDialog.Filter = "Excel文档(*.xls)|*xls";
                fileDialog.RestoreDirectory = true;
                fileDialog.FileName = string.Format("{0:yyyyMMdd}.xls", DateTime.Now);
                var result = fileDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string localFilePath = fileDialog.FileName;
                    using (var workbook = new Common.Excel.Workbook())
                    {
                        var sheet = new Common.Excel.Sheet("数据", columns, dt);
                        workbook.CreateSheet(sheet);

                        using (var ms = workbook.GetMemoryStream())
                        {
                            var fs = new System.IO.FileStream(localFilePath, System.IO.FileMode.CreateNew);
                            byte[] buff = ms.ToArray();
                            fs.Write(buff, 0, buff.Length);
                            fs.Flush();
                            fs.Close();
                        }
                    }

                    MessageBox.Show("保存文件并写入内容成功。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetConnetionString(string dbName = "master")
        {
            string dbIp = this.txtIP.Text.Trim();
            string dbUser = this.txtUserName.Text.Trim();
            string dbPassword = this.txtPassword.Text.Trim();

            var connStr = new SqlConnectionStringBuilder()
            {
                DataSource = dbIp,
                InitialCatalog = dbName,
                IntegratedSecurity = false,
                UserID = dbUser,
                Password = dbPassword,

                Pooling = true, //开启连接池
                MinPoolSize = 0,//设置最小连接数为0
                MaxPoolSize = 2000, //设置最大连接数为50             
                ConnectTimeout = 15, //设置超时时间为15秒
            };
            return connStr.ConnectionString;
        }

        public DataTable Query(string connectionString, string sql)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                DataSet ds = new DataSet();
                try
                {
                    connection.Open();
                    SqlDataAdapter command = new SqlDataAdapter(sql, connection);
                    command.Fill(ds, "ds");
                }
                catch (SqlException ex)
                {
                    throw ex;
                }
                return ds.Tables[0];
            }
        }

        private void AddGroupTitleSetControl(int columnsCount)
        {
            ColumnY = ColumnDefaultY;
            this.panel1.Controls.Clear();
            for (int i = 1; i <= columnsCount; i++)
            {
                AddOneGroupTitleSetControl(i);
            }
        }

        private void AddOneGroupTitleSetControl(int index)
        {
            int columnY = ColumnY;
            this.panel1.Controls.Add(new Label { Name = "lbl_index_" + index, Text = "第" + index + "列标题", AutoSize = true, Location = new Point { X = 5, Y = columnY } });
            this.panel1.Controls.Add(new TextBox { Name = "txt_title_" + index, Text = "", Width = 100, Location = new Point { X = 65, Y = columnY } });
            this.panel1.Controls.Add(new Label { Name = "lbl_type_" + index, Text = "第" + index + "列格式", AutoSize = true, Location = new Point { X = 170, Y = columnY } });
            this.panel1.Controls.Add(new ComboBox { Name = "cmb_type_" + index, Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point { X = 230, Y = columnY }, DataSource = ColumnDataTypes.Select(o => o.Key).ToList() });
            this.panel1.Controls.Add(new Label { Name = "lbl_index_" + index, Text = "第" + index + "列宽度", AutoSize = true, Location = new Point { X = 330, Y = columnY } });
            this.panel1.Controls.Add(new NumericUpDown { Name = "num_width_" + index, Minimum = 8, Maximum = 150, Width = 100, Location = new Point { X = 400, Y = columnY } });
            this.panel1.Controls.Add(new Label { Name = "lbl_error_" + index, Text = "", AutoSize = true, ForeColor = Color.Red, Location = new Point { X = 500, Y = columnY } });
            ColumnY += 30;
        }

        private List<dynamic> GetGroupTitleSet(int columnsCount)
        {
            var list = new List<dynamic>();
            for (int i = 1; i <= columnsCount; i++)
            {
                var lblIndex = this.panel1.Controls.Find("lbl_index_" + i, false)[0] as Label;
                string index = lblIndex.Text;

                var txtTitle = this.panel1.Controls.Find("txt_title_" + i, false)[0] as TextBox;
                string title = txtTitle.Text;

                var cmbType = this.panel1.Controls.Find("cmb_type_" + i, false)[0] as ComboBox;
                string type = cmbType.SelectedItem.ToString();

                var numWidth = this.panel1.Controls.Find("num_width_" + i, false)[0] as NumericUpDown;
                int width = (int)numWidth.Value;

                var lblError = this.panel1.Controls.Find("lbl_error_" + i, false)[0] as Label;
                if (string.IsNullOrEmpty(title))
                {
                    lblError.Text = index + "不能为空";
                }

                list.Add(new { Title = title, Type = type, Width = width });
            }
            return list;
        }
    }
}
