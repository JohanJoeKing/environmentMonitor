using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

/**********************************************
 * - Project name : EnvironmentManagerServer
 * - Filename : FormAddUser.cs
 * - Author : 刘畅    Version : 1.0   Date : 2018-3-25
 * - Description : // 地铁站隧道安全监测系统-服务器部分
 * - Function list:
 * 1.FormAddUser
 * 2.buttonSubmit_Click
 * - Others : // 
 **********************************************/

namespace EnvironmentManagerServer
{
    public partial class FormAddUser : Form
    {
        /*
         * TODO 数据库服务
         * 
         */
        private MyDatabseManager DB;



        /*************************************
         * Function name : FormAddUser
         * Description : 构造函数
         * Variables : void
         *************************************/
        public FormAddUser()
        {
            InitializeComponent();
            DB = new MyDatabseManager();
        }



        /*************************************
         * Function name : buttonSubmit_Click
         * Description : 提交
         * Variables : object sender, EventArgs e
         *************************************/
        private void buttonSubmit_Click(object sender, EventArgs e)
        {
            // 收集信息
            string ip = textBoxIP.Text.ToString();
            string name = textBoxName.Text.ToString();
            string phone = textBoxPhone.Text.ToString();

            // 插入
            if (DB.addUser(name, phone, ip))
            {
                MessageBox.Show("添加成功");
            }
            else
            {
                MessageBox.Show("添加失败");
            }
        }
    }
}
