using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Configuration;

/**********************************************
 * - Project name : EnvironmentManagerServer
 * - Filename : MyDatabaseManager.cs
 * - Author : 刘畅    Version : 1.0   Date : 2018-3-25
 * - Description : // 地铁站隧道安全监测系统-服务器部分
 * - Function list:
 * 1.MyDatabseManager
 * 2.getPhoneName
 * 3.addUser
 * 4.getIP
 * - Others : // 
 **********************************************/

namespace EnvironmentManagerServer
{
    class MyDatabseManager
    {



        /*************************************
         * Function name : MyDatabaseManager
         * Description : 构造函数
         * Variables : void
         *************************************/
        public MyDatabseManager()
        {}



        /*************************************
         * Function name : MyDatabaseManager
         * Description : 查询IP姓名电话
         * Variables : void
         *************************************/
        public string getPhoneName(string ip)
        {
            string msg = "";
            string phone = "", name = "";

            // 数据库连接
            string constr = "server=localhost;User Id=root;password=123456;Database=evmm";
            MySqlConnection mycon = new MySqlConnection(constr);
            mycon.Open();
            string sql = string.Format("select phone, name from user where ip='{0}'", ip);
            MySqlCommand mycmd = new MySqlCommand(sql, mycon);
            MySqlDataReader reader = null;
            reader = mycmd.ExecuteReader();
            while (reader.Read())
            {
                phone = reader[0].ToString();
                name = reader[1].ToString();
            }
            reader.Close();
            mycon.Close();

            msg = phone + " " + name;
            return msg;
        }




        /*************************************
         * Function name : addUser
         * Description : 添加用户
         * Variables : void
         *************************************/
        public bool addUser(string name, string phone, string ip)
        {
            bool flag = false;
            string constr = "server=localhost;User Id=root;password=123456;Database=evmm";
            MySqlConnection mycon = new MySqlConnection(constr);
            mycon.Open();
            string sql = string.Format("insert into user values('{0}','{1}','{2}')", ip, phone, name);
            MySqlCommand mycmd = new MySqlCommand(sql, mycon);
            if (mycmd.ExecuteNonQuery() > 0)
            {
                flag = true;
            }
            mycon.Close();
            return flag;
        }




        /*************************************
         * Function name : getIP
         * Description : 查询用户IP
         * Variables : void
         *************************************/
        public string getIP(string phone)
        {
            string msg = "";

            // 数据库连接
            string constr = "server=localhost;User Id=root;password=123456;Database=evmm";
            MySqlConnection mycon = new MySqlConnection(constr);
            mycon.Open();
            string sql = string.Format("select ip from user where phone={0}", phone);
            MySqlCommand mycmd = new MySqlCommand(sql, mycon);
            MySqlDataReader reader = null;
            reader = mycmd.ExecuteReader();
            while (reader.Read())
            {
                msg = reader[0].ToString();
            }
            reader.Close();
            mycon.Close();

            return msg;
        }
    }
}
