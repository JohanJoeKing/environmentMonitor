using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

/**********************************************
 * - Project name : EnvironmentManagerServer
 * - Filename : Form1.cs
 * - Author : 刘畅    Version : 1.0   Date : 2018-3-25
 * - Description : // 地铁站隧道安全监测系统-服务器部分
 * - Function list:
 * 1.Form1
 * 2.init
 * 3.login
 * 4.offline
 * 5.serverThread
 * 6.dataThread
 * 7.loadData
 * 8.button1_Click
 * 9.buttonChangeLimit_Click
 * 10.buttonInitLimit_Click
 * 11.buttonAlert1_Click
 * 12.buttonAlert2_Click
 * 13.launchAlert
 * 14.buttonSend_Click
 * 15.buttonClearAlert_Click
 * 16.radioButtonOpen_CheckedChanged
 * 17.radioButtonClose_CheckedChanged
 * 18.buttonAllSend_Click
 * 19.buttonAddUser_Click
 * - Others : // 
 **********************************************/

// 方法注释模板
/*************************************
 * Function name : 
 * Description : 
 * Variables : 
 *************************************/

namespace EnvironmentManagerServer
{
    public partial class Form1 : Form
    {
        /*
         * 服务器向移动端发送的数据只使用LINK_PORT端口
         * 其数据格式协议为：
         * 1、alert开头为预警消息
         * 2、[1]开头为普通通讯
         * 3、[2]开头为数据下载回馈
         * 
         */




        /*
         * TODO 系统支持项
         * 
         */
        private String[] user;              // 已登录用户IP缓存
        private bool[] u_available;         // 缓存位可用，true表示可用
        private const int USER_MAX = 100;   // 服务器支持的用户连接上限
        private int onlineAmount;           // 在线用户数

        private double[] temp;            // 温度
        private double[] humi;            // 湿度
        private double[] smoke;           // 烟雾浓度
        private double[] press;           // 岩土层压力
        private const int SENSORS = 4;    // 传感器个数
        private double tempRise;          // 升温阈值
        private double tempDepress;       // 降温阈值
        private double humiRise;          // 加湿阈值
        private double humiDepress;       // 减湿阈值
        private double smokeLimit;        // 烟雾浓度阈值
        private double pressLimit;        // 岩土层压力阈值
        private double[] t2;              // 温度变化缓存
        private double[] h2;              // 湿度变化缓存
        private double[] s2;              // 烟雾浓度变化缓存
        private double[] p2;              // 岩土层压力变化缓存
        private int buffers;              // 缓存数

        private Thread ST;    // 登录申请监听线程
        private Thread DT;    // 数据回传监听线程

        private const int LOGIN_PORT = 30010;  // 登录线程端口
        private const int DATA_PORT = 30011;   // 数据接收端口
        private const int LINK_PORT = 30012;   // 通讯端口

        private bool AUTO_ALERT;       // 自动报警状态是否打开




        /*
         * TODO 数据库服务
         * 
         */
        private MyDatabseManager DB;



        /*
         * TODO 相关控件
         * 
         */
        private FormAddUser FAU;


        /*************************************
         * Function name : Form1
         * Description : 构造函数
         * Variables : void
         *************************************/
        public Form1()
        {
            InitializeComponent();
            DB = new MyDatabseManager();
            init();
        }



        /*************************************
         * Function name : init
         * Description : 系统初始化
         * Variables : void
         *************************************/
        private void init()
        {
            // 系统支持项初始化
            // 用户处理
            user = new String[USER_MAX];
            u_available = new bool[USER_MAX];
            for (int i = 0; i < USER_MAX; i++)
            {
                user[i] = "";            // 缓存位置空
                u_available[i] = true;   // 可用状态初始化为可用
            }
            onlineAmount = 0;            // 在线用户为0个
            // 环境监测处理
            temp = new double[SENSORS];
            humi = new double[SENSORS];
            smoke = new double[SENSORS];
            press = new double[SENSORS];
            for (int i = 0; i < SENSORS;i++ ){
                temp[i] = humi[i] = smoke[i] = press[i] = 0;
            }
            /*
             * 默认阈值：
             * 升温：3     降温：5
             * 加湿：5     减湿：10
             * 烟雾极限：5
             * 岩土层压力极限：3
             */
            tempRise = 3;
            tempDepress = 5;
            humiRise = 5;
            humiDepress = 10;
            smokeLimit = 5;
            pressLimit = 3;
            textBoxRiseTemp.Text = tempRise.ToString();
            textBoxRiseHumi.Text = tempDepress.ToString();
            textBoxDepressTemp.Text = humiRise.ToString();
            textBoxDepressHumi.Text = humiDepress.ToString();
            textBoxSmokeLimit.Text = smokeLimit.ToString();
            textBoxPressLimit.Text = pressLimit.ToString();
            // 报警机制
            AUTO_ALERT = false;
            radioButtonClose.Checked = true;
            // 数据缓存
            t2 = new double[SENSORS];
            h2 = new double[SENSORS];
            s2 = new double[SENSORS];
            p2 = new double[SENSORS];
            for (int i = 0; i < SENSORS; i++)
            {
                t2[i] = h2[i] = s2[i] = p2[i] = 0;
            }
            buffers = 0;

            // 开启线程
            ST = new Thread(serverThread);
            ST.Start();
            DT = new Thread(dataThread);
            DT.Start();
        }



        /*************************************
         * Function name : login
         * Description : 登录操作
         * Variables : String ip
         *************************************/
        public void login(String ip)
        {
            // 检查登录数是否达上限
            if (onlineAmount == USER_MAX)
            {
                return;
            }

            // 检查申请IP是否已登录，否则不予操作
            for (int i = 0; i < USER_MAX; i++)
            {
                if (u_available[i])
                {
                    continue;
                }
                if (user[i] == ip)
                {
                    return;
                }
            }

            // 添加ip到缓存数组
            int index = 0;
            for (int i = 0; i < USER_MAX; i++)
            {
                // 查找最近的能用的位置
                if (u_available[i])
                {
                    index = i; // 确认插入位置
                    break;
                }
            }
            user[index] = ip;           // 插入缓存
            u_available[index] = false; // 封锁缓存位
            onlineAmount++;             // 在线登录数++
            
            // 显示在列表
            string str = DB.getPhoneName(ip);
            if (str != "")
            {
                listBoxUser.Items.Add(str);
            }
            else
            {
                listBoxUser.Items.Add(ip);
            }
        }



        /*************************************
         * Function name : offline
         * Description : 下线操作
         * Variables : String ip
         *************************************/
        public void offline(String ip)
        {
            // 检查缓存是否已空
            if (onlineAmount == 0)
            {
                return;
            }

            // 检查申请IP是否已下线，未下线的话保存位置
            int n = 0;
            int index = 0;
            for (int i = 0; i < USER_MAX; i++)
            {
                if (!u_available[i])
                {
                    if (user[i] == ip)
                    {
                        n++;
                        index = i;
                        break;
                    }
                }
                else
                {
                    continue;
                }
            }
            if (n == 0)
            {
                return;
            }

            // 下线操作
            user[index] = "";            // 缓存位置空
            u_available[index] = true;   // 可用性打开
            onlineAmount--;              // 在线登录数--
            
            // 撤销显示
            string str = DB.getPhoneName(ip);
            if (str != "")
            {
                listBoxUser.Items.Remove(str);
            }
            else
            {
                listBoxUser.Items.Remove(ip);
            }
        }



        /*************************************
         * Function name : serverThread
         * Description : 登录申请监听线程
         * Variables : void
         *************************************/
        public void serverThread()
        {
            try
            {
                UdpClient udpclient = new UdpClient(LOGIN_PORT);
                while (true)
                {
                    IPEndPoint ipendpoint = null;

                    //停在这等待数据
                    byte[] bytes = udpclient.Receive(ref ipendpoint);   

                    string data = Encoding.Default.GetString(bytes, 0, bytes.Length);

                    //MessageBox.Show("get:" + data);

                    // 数据协议解码
                    /* 规定：
                     * data值第一位表示command
                     * 后面的部分表示ip
                     * command值：
                     * 1：登录申请
                     * 2：下线申请
                     * 3：数据下载申请
                     */
                    string ip, command;
                    command = data.Substring(0, 1);
                    ip = data.Substring(1, data.Length - 1);
                    if (command == "1")
                    {
                        // 登录
                        login(ip);
                    }
                    else if (command == "2")
                    {
                        // 下线
                        offline(ip);
                    }
                    else if (command == "3")
                    {
                        // 环境数据下载请求
                        loadData(ip);
                    }

                }
            }
            catch (Exception e)
            {
                e.GetBaseException();
            } 
        }



        /*************************************
         * Function name : dataThread
         * Description : 传感器回传数据监听线程
         * Variables : void
         *************************************/
        public void dataThread()
        {
            try
            {
                UdpClient udpclient = new UdpClient(DATA_PORT);
                while (true)
                {
                    IPEndPoint ipendpoint = null;

                    //停在这等待数据
                    byte[] bytes = udpclient.Receive(ref ipendpoint);

                    string data = Encoding.Default.GetString(bytes, 0, bytes.Length);

                    /**
                    * 数据协议规定：
                    * 1、数据顺序为：温度、湿度、烟雾浓度、岩土层压力
                    * 2、每块数据长度均为3个字符
                    * 3、不够标准字符长的数据算法补齐
                    * 4、按传感器编号顺序内循环数据顺序
                    */
                    string[] buffer = new string[SENSORS * 4];
                    int index = 0;
                    for (int i = 0; i < SENSORS * 4; i++)
                    {
                        buffer[i] = data.Substring(index, 5);
                        index += 5;
                    }
                    index = 0;
                    for (int i = 0; i < SENSORS; i++)
                    {
                        t2[i] = temp[i];
                        temp[i] = double.Parse(buffer[index++]);
                        t2[i] = temp[i] - t2[i];

                        h2[i] = humi[i];
                        humi[i] = double.Parse(buffer[index++]);
                        h2[i] = humi[i] - h2[i];

                        s2[i] = smoke[i];
                        smoke[i] = double.Parse(buffer[index++]);
                        s2[i] = smoke[i] - s2[i];

                        p2[i] = press[i];
                        press[i] = double.Parse(buffer[index++]);
                        p2[i] = press[i] - p2[i];
                    }


                    // 缓存数据并分析
                    for (int i = 0; i < SENSORS; i++)
                    {
                        // 缓存为0跳过
                        if (buffers == 0)
                        {
                            break;
                        }

                        // 温度异常
                        if ((t2[i] > 0 && t2[i] > tempRise)
                            || (t2[i] < 0 && t2[i] < tempDepress))
                        {
                            listBoxAlert.Items.Add("[" + i + "]区温度变化异常[" + System.DateTime.Now + "]");
                        }
                        
                        // 湿度异常
                        if ((h2[i] > 0 && h2[i] > humiRise)
                            || (h2[i] < 0 && h2[i] < humiDepress))
                        {
                            listBoxAlert.Items.Add("[" + i + "]区湿度变化异常[" + System.DateTime.Now + "]");
                            if (AUTO_ALERT)
                            {
                                launchAlert(1);
                            }
                        }
                        // 烟雾浓度异常
                        if (s2[i] > smokeLimit)
                        {
                            listBoxAlert.Items.Add("[" + i + "]区烟雾浓度变化异常[" + System.DateTime.Now + "]");
                            if (AUTO_ALERT)
                            {
                                launchAlert(2);
                            }
                        }
                        // 岩土层压力异常
                        if (p2[i] > pressLimit)
                        {
                            listBoxAlert.Items.Add("[" + i + "]区岩土层压力变化异常[" + System.DateTime.Now + "]");
                            if (AUTO_ALERT)
                            {
                                launchAlert(1);
                            }
                        }
                    }

                    // 显示
                    textBoxTemp1.Text = temp[0].ToString();
                    textBoxTemp2.Text = temp[1].ToString();
                    textBoxTemp3.Text = temp[2].ToString();
                    textBoxTemp4.Text = temp[3].ToString();
                    textBoxHumi1.Text = humi[0].ToString();
                    textBoxHumi2.Text = humi[1].ToString();
                    textBoxHumi3.Text = humi[2].ToString();
                    textBoxHumi4.Text = humi[3].ToString();
                    textBoxSmoke1.Text = smoke[0].ToString();
                    textBoxSmoke2.Text = smoke[1].ToString();
                    textBoxSmoke3.Text = smoke[2].ToString();
                    textBoxSmoke4.Text = smoke[3].ToString();
                    textBoxPress1.Text = press[0].ToString();
                    textBoxPress2.Text = press[1].ToString();
                    textBoxPress3.Text = press[2].ToString();
                    textBoxPress4.Text = press[3].ToString();

                    buffers++;
                }// while
            }
            catch (Exception e)
            {
                e.GetBaseException();
            }
        }



        /*************************************
         * Function name : loadData
         * Description : 用户下载环境数据
         * Variables : string ip
         *************************************/
        public void loadData(string ip)
        {
            // 数据打包
            string msg;
            msg = "[2]";
            /**
             * 数据协议规定：
            * 1、数据顺序为：温度、湿度、烟雾浓度、岩土层压力
            * 2、每块数据长度均为3个字符
            * 3、不够标准字符长的数据算法补齐
            * 4、按传感器编号顺序内循环数据顺序
            */
            string[] d = new string[SENSORS];
            for (int i = 0; i < SENSORS; i++)
            {
                d[i] = "";
            }
            for (int i = 0; i < SENSORS; i++)
            {
                if (temp[i] < 10 && temp[i] > 0)
                {
                    d[i] = "00" + temp[i];
                }
                else
                {
                    d[i] = "0" + temp[i];
                }
                if (humi[i] < 10 && humi[i] > 0)
                {
                    d[i] += ("00" + humi[i]);
                }
                else
                {
                    d[i] += ("0" + humi[i]);
                }
                if (smoke[i] < 10)
                {
                    d[i] += ("00" + smoke[i]);
                }
                else
                {
                    d[i] += ("0" + smoke[i]);
                }
                if (press[i] < 10)
                {
                    d[i] += ("00" + press[i]);
                }
                else
                {
                    d[i] += ("0" + press[i]);
                }
            }
            for (int i = 0; i < SENSORS; i++)
            {
                msg += d[i];
            }

            // 数据发送
            try
            {
                UdpClient udpclient = new UdpClient();
                IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Parse(ip), LINK_PORT);
                byte[] data = Encoding.Default.GetBytes(msg);
                udpclient.Send(data, data.Length, ipendpoint);
                udpclient.Close();
            }
            catch (Exception ex)
            {
                ex.GetBaseException();
            }
        }



        /*************************************
         * Function name : button1_Click
         * Description : 刷新用户列表
         * Variables : object sender, EventArgs e
         *************************************/
        private void button1_Click(object sender, EventArgs e)
        {
            // 清空列表
            listBoxUser.Items.Clear();

            // 添加登录IP
            for (int i = 0; i < USER_MAX; i++)
            {
                if(!u_available[i])
                    listBoxUser.Items.Add(user[i]);
            }
        }

        private void label18_Click(object sender, EventArgs e)
        {}




        /*************************************
         * Function name : buttonChangeLimit_Click
         * Description : 应用环境阈值
         * Variables : object sender, EventArgs e
         *************************************/
        private void buttonChangeLimit_Click(object sender, EventArgs e)
        {
            // 收集信息
            tempRise = double.Parse(textBoxRiseTemp.Text.ToString());
            tempDepress = double.Parse(textBoxRiseTemp.Text.ToString());
            humiRise = double.Parse(textBoxRiseTemp.Text.ToString());
            humiDepress = double.Parse(textBoxRiseTemp.Text.ToString());
            smokeLimit = double.Parse(textBoxRiseTemp.Text.ToString());
            pressLimit = double.Parse(textBoxRiseTemp.Text.ToString());

            // 提示
            MessageBox.Show("修改阈值完成。");
        }




        /*************************************
         * Function name : buttonInitLimit_Click
         * Description : 恢复默认环境阈值
         * Variables : object sender, EventArgs e
         *************************************/
        private void buttonInitLimit_Click(object sender, EventArgs e)
        {
            /*
             * 默认阈值：
             * 升温：3     降温：5
             * 加湿：5     减湿：10
             * 烟雾极限：5
             * 岩土层压力极限：3
             */
            tempRise = 3;
            tempDepress = 5;
            humiRise = 5;
            humiDepress = 10;
            smokeLimit = 5;
            pressLimit = 3;
            textBoxRiseTemp.Text = tempRise.ToString();
            textBoxRiseHumi.Text = tempDepress.ToString();
            textBoxDepressTemp.Text = humiRise.ToString();
            textBoxDepressHumi.Text = humiDepress.ToString();
            textBoxSmokeLimit.Text = smokeLimit.ToString();
            textBoxPressLimit.Text = pressLimit.ToString();
            MessageBox.Show("已恢复默认阈值。");
        }




        /*************************************
         * Function name : buttonAlert1_Click
         * Description : 发布地质预警
         * Variables : object sender, EventArgs e
         *************************************/
        private void buttonAlert1_Click(object sender, EventArgs e)
        {
            launchAlert(1);
        }




        /*************************************
         * Function name : buttonAlert2_Click
         * Description : 发布消防预警
         * Variables : object sender, EventArgs e
         *************************************/
        private void buttonAlert2_Click(object sender, EventArgs e)
        {
            launchAlert(2);
        }



        /*************************************
         * Function name : launchAlert
         * Description : 发布预警
         * Variables : int state
         *************************************/
        public void launchAlert(int state)
        {
            // 预警类型state：1、地质类     2、消防类
            string flag = "";
            if (state == 1)
            {
                flag = "alert1";
            }
            else
            {
                flag = "alert2";
            }

            // 遍历登录用户IP
            for (int i = 0; i < USER_MAX; i++)
            {
                if (u_available[i])
                {
                    continue;
                }
                else
                {
                    try
                    {
                        UdpClient udpclient = new UdpClient();
                        IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Parse(user[i]), LINK_PORT);
                        byte[] data = Encoding.Default.GetBytes(flag);
                        udpclient.Send(data, data.Length, ipendpoint);
                        udpclient.Close();
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
            }
        }




        /*************************************
         * Function name : buttonSend_Click
         * Description : 单线联系发送
         * Variables : object sender, EventArgs e
         *************************************/
        private void buttonSend_Click(object sender, EventArgs e)
        {
            // 收集信息
            string phone = textBoxUserIP.Text.ToString();
            string uip = DB.getIP(phone);
            string content = textBoxContent.Text.ToString();

            // 检查IP用户是否已登录
            bool flag = false;
            for (int i = 0; i < USER_MAX; i++)
            {
                if (u_available[i])
                {
                    continue;
                }
                else
                {
                    if (user[i] == uip)
                    {
                        flag = true;
                        break;
                    }
                }
            }
            if (!flag)
            {
                MessageBox.Show("该用户未登录或未注册！");
                return;
            }

            // 发送消息
            try
            {
                UdpClient udpclient = new UdpClient();
                IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Parse(uip), LINK_PORT);
                byte[] data = Encoding.Default.GetBytes("[1]" + content);
                udpclient.Send(data, data.Length, ipendpoint);
                udpclient.Close();
            }
            catch (Exception ex)
            {
                ex.GetBaseException();
            }
        }




        /*************************************
         * Function name : buttonClearAlert_Click
         * Description : 清空预警记录
         * Variables : object sender, EventArgs e
         *************************************/
        private void buttonClearAlert_Click(object sender, EventArgs e)
        {
            listBoxAlert.Items.Clear();
        }




        /*************************************
         * Function name : radioButtonOpen_CheckedChanged
         * Description : 开启自动报警模式
         * Variables : object sender, EventArgs e
         *************************************/
        private void radioButtonOpen_CheckedChanged(object sender, EventArgs e)
        {
            buttonAlert1.Enabled = false;
            buttonAlert2.Enabled = false;
            AUTO_ALERT = true;
        }




        /*************************************
         * Function name : radioButtonClose_CheckedChanged
         * Description : 关闭自动报警模式
         * Variables : object sender, EventArgs e
         *************************************/
        private void radioButtonClose_CheckedChanged(object sender, EventArgs e)
        {
            buttonAlert1.Enabled = true;
            buttonAlert2.Enabled = true;
            AUTO_ALERT = false;
        }



        /*************************************
         * Function name : buttonAllSend_Click
         * Description : 消息群发
         * Variables : object sender, EventArgs e
         *************************************/
        private void buttonAllSend_Click(object sender, EventArgs e)
        {
            // 收集信息
            string msg = "[1]" + textBoxContent.Text.ToString();

            // 遍历登录用户IP
            for (int i = 0; i < USER_MAX; i++)
            {
                if (u_available[i])
                {
                    continue;
                }
                else
                {
                    try
                    {
                        UdpClient udpclient = new UdpClient();
                        IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Parse(user[i]), LINK_PORT);
                        byte[] data = Encoding.Default.GetBytes(msg);
                        udpclient.Send(data, data.Length, ipendpoint);
                        udpclient.Close();
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
            }

            // 修改UI
            textBoxContent.Text = "";
        }





        /*************************************
         * Function name : buttonAddUser_Click
         * Description : 添加用户
         * Variables : object sender, EventArgs e
         *************************************/
        private void buttonAddUser_Click(object sender, EventArgs e)
        {
            FAU = new FormAddUser();
            FAU.Show();
        }
    }
}
