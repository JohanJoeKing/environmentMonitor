package com.example.johan.environmentmanagerapp;

/******************************************************
 - Project name : EnvironmentManagerAPP
 - File name : MainActivity.java
 - Author : 刘畅    Version : 1.0   Date : 2018-3-25
 - Description : // 地铁站隧道安全监测系统-客户端部分
                 // 接收服务器反馈的各种数据并显示
                 // 本文件为主界面文件
 - Function list :
 1.onCreate
 2.onDestroy
 3.linkToServer
 4.getIP
 5.analyseData
 6.changeDataPrint
 7.callDataApply
 8.radio1_Click
 9.radio2_Click
 10.radio3_Click
 11.radio4_Click
 12.assure_Click
 - History : //
 *****************************************************/


//// TODO 文件头注释模板
/******************************************************
 - Project name :
 - File name :
 - Author : Liuchang    Version : 1.0   Date :
 - Description : //
 - Others : //
 - Function list :
 1.
 - History : //
 *****************************************************/

//// TODO 函数头注释模板
/*******************************
 - Function name :
 - Description :
 - Variables :
 *******************************/

import android.os.Handler;
import android.os.Message;
import android.os.Vibrator;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.view.View;
import android.widget.TextView;
import android.widget.Toast;

import java.io.IOException;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.Inet4Address;
import java.net.InetAddress;
import java.net.NetworkInterface;
import java.net.SocketException;
import java.util.Enumeration;

public class MainActivity extends AppCompatActivity {

    /****************************************
     *
     * 服务器向移动端发送的数据只使用LINK_PORT端口
     * 其服务器反馈数据格式协议为：
     * 1、alert开头为预警消息
     * 2、[1]开头为普通通讯
     * 3、[2]开头为数据下载回馈
     *
     * 用户端发送请求格式：
     * 1、第一位为请求类型，1：登录  2：数据下载
     * 2、后跟本设备IP
     *
     ****************************************
     */


    /**
     * TODO 系统支持项
     */
    private final int SENSORS = 4;    // 传感器数量
    private double[] temp;    // 实时数据
    private double[] humi;
    private double[] press;
    private double[] smoke;
    private int area;             // 当前显示区域号[0-3]
    private boolean vibrateState; // 振动状态


    /**
     * TODO 控件
     */
    private TextView ttemp = null;
    private TextView thumi = null;
    private TextView tsmoke = null;
    private TextView tpress = null;
    private TextView tinfo = null;
    private Vibrator vibrator;
    long[] patter = {500, 1000, 500, 1000};
    long[] short_patter = {1000, 500, 1000};


    /**
     * TODO 线程服务
     */
    private Handler handler = null;
    private linkThread LT;
    private dataLoadThread DLT;

    /**
     * TODO 网络服务
     */
    private String HOST_IP = "192.168.43.62";
    private final int LINK_PORT = 30012;         // 通讯端口
    private final int LOGIN_PORT = 30010;        // 登录端口



    /*******************************
     - Function name : onCreate
     - Description : 构造函数
     - Variables : Bundle savedInstanceState
     *******************************/
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        // 绑定控件
        ttemp = (TextView) findViewById(R.id.textView);
        thumi = (TextView) findViewById(R.id.textView2);
        tsmoke = (TextView) findViewById(R.id.textView3);
        tpress = (TextView) findViewById(R.id.textView4);
        tinfo = (TextView) findViewById(R.id.textView6);
        vibrator = (Vibrator)this.getSystemService(this.VIBRATOR_SERVICE);


        // 初始化实时缓存
        temp = new double[SENSORS];
        humi = new double[SENSORS];
        smoke = new double[SENSORS];
        press = new double[SENSORS];
        for(int i = 0;i < SENSORS;i++){
            temp[i] = humi[i] = smoke[i] = press[i] = 0;
        }
        area = 0;
        vibrateState = false;


        // 连接到服务器
        linkToServer();

        // 绑定线程更改UI
        handler = new Handler(){
            public void handleMessage(Message msg) {
                super.handleMessage(msg);
                switch (msg.what) {
                    case 0:
                        //在这里得到数据，并且可以直接更新UI
                        String data = (String)msg.obj;
                        analyseData(data);
                        break;
                    default:
                        break;
                }
            }
        };

        // 启动线程
        LT = new linkThread(handler);
        LT.start();
        DLT = new dataLoadThread();
        DLT.start();

        // 第一次请求数据
        callDataApply();
    }

    /*******************************
     - Function name : onDestroy
     - Description : 退出时清理操作
     - Variables : void
     *******************************/
    @Override
    public void onDestroy(){
        super.onDestroy();

        // 向服务器发送下线请求
        try {
            String msg = "2" + getIP();
            byte[] buf= msg.getBytes();
            DatagramSocket sendSocket = new DatagramSocket();
            InetAddress serverAddr = InetAddress.getByName(HOST_IP);
            DatagramPacket outPacket = new DatagramPacket(buf, buf.length,serverAddr, LOGIN_PORT);
            sendSocket.send(outPacket);
            sendSocket.close();
        } catch (SocketException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }



    /*******************************
     - Function name : TODO linkToServer
     - Description : 连接到服务器
     - Variables : void
     *******************************/
    private void linkToServer(){
        try {
            String msg = "1" + getIP();
            byte[] buf= msg.getBytes();
            DatagramSocket sendSocket = new DatagramSocket();
            InetAddress serverAddr = InetAddress.getByName(HOST_IP);
            DatagramPacket outPacket = new DatagramPacket(buf, buf.length,serverAddr, LOGIN_PORT);
            sendSocket.send(outPacket);
            sendSocket.close();
        } catch (SocketException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }finally {
            Toast.makeText(this,"已连接到服务器",Toast.LENGTH_LONG).show();
        }
    }



    /*******************************
     - Function name : getIP
     - Description : 获取本机IPv4地址
     - Variables : void
     *******************************/
    public String getIP(){
        try {
            for (Enumeration<NetworkInterface> en = NetworkInterface.getNetworkInterfaces(); en.hasMoreElements();) {
                NetworkInterface intf = en.nextElement();
                for (Enumeration<InetAddress> enumIpAddr = intf.getInetAddresses(); enumIpAddr.hasMoreElements();)
                {
                    InetAddress inetAddress = enumIpAddr.nextElement();
                    if (!inetAddress.isLoopbackAddress() && (inetAddress instanceof Inet4Address))
                    {
                        return inetAddress.getHostAddress().toString();
                    }
                }
            }
        }
        catch (SocketException ex){
            ex.printStackTrace();
        }
        return null;
    }



    /*******************************
     - Function name : analyseData
     - Description : 分析数据
     - Variables : String data
     *******************************/
    public void analyseData(String data){
        // 检查是否为预警
        if(data.equals("alert1") || data.equals("alert2")){
            if(data.equals("alert1")){
                tinfo.setText("[调度中心]地质预警");
            }
            else{
                tinfo.setText("[调度中心]火警预警");
            }
            vibrator.vibrate(patter, 0);
            vibrateState = true;
        }

        // 检查申请类型
        String command = data.substring(0,3);

        // 分类执行
        if(command.equals("[1]")){
            // 普通通讯
            String msg = data.substring(3, data.length());
            tinfo.setText("[调度中心]" + msg);
            vibrator.vibrate(short_patter, -1);
        }


        else if(command.equals("[2]")){
            // 数据下载
            // 分离数据
            String[] d = new String[SENSORS * 4];
            int index = 3;
            for(int i = 0;i < SENSORS * 4;i++){
                d[i] = data.substring(index, index + 3);
                index += 3;
            }
            index = 0;
            for(int i = 0;i < SENSORS;i++){
                temp[i] = Double.parseDouble(d[index++]);
                humi[i] = Double.parseDouble(d[index++]);
                smoke[i] = Double.parseDouble(d[index++]);
                press[i] = Double.parseDouble(d[index++]);
            }

            // 控件显示
            changeDataPrint();
        }
    }



    /*******************************
     - Function name : changeDataPrint
     - Description : 修改UI
     - Variables : void
     ******************************/
    public void changeDataPrint(){
        ttemp.setText("温度：" + temp[area] + "度");
        thumi.setText("湿度：" + humi[area] + "%");
        tsmoke.setText("烟雾浓度：" + smoke[area] + "ml/m3");
        tpress.setText("岩土层压力："  + press[area] + "pa");
    }



    /*******************************
     - Function name : changeDataPrint
     - Description : 修改UI
     - Variables : void
     ******************************/
    public void callDataApply(){
        try {
            String msg = "3" + getIP();
            byte[] buf= msg.getBytes();
            DatagramSocket sendSocket = new DatagramSocket();
            InetAddress serverAddr = InetAddress.getByName(HOST_IP);
            DatagramPacket outPacket = new DatagramPacket(buf, buf.length,serverAddr, LOGIN_PORT);
            sendSocket.send(outPacket);
            sendSocket.close();
        } catch (SocketException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }




    /*******************************
     - Function name : radio1_Click/radio2_Click/radio3_Click/radio4_Click
     - Description : 点击单选按钮
     - Variables : View v
     ******************************/
    public void radio1_Click(View v){
        area = 0;
        changeDataPrint();
    }
    public void radio2_Click(View v){
        area = 1;
        changeDataPrint();
    }
    public void radio3_Click(View v){
        area = 2;
        changeDataPrint();
    }
    public void radio4_Click(View v){
        area = 3;
        changeDataPrint();
    }




    /*******************************
     - Function name : assure_Click
     - Description : 收到确认
     - Variables : View v
     ******************************/
    public void assure_Click(View v){
        if(vibrateState){
            vibrator.cancel();
            vibrateState = false;
            tinfo.setText("[调度中心]");
        }
        else{
            return;
        }
    }




    /******************************************************************
     *
     * 通讯接收线程
     *
     */
    public class linkThread extends Thread{

        /**
         * TODO 线程内部支持项
         */
        private Handler handler;
        private String data;


        /*******************************
         - Function name : linkThread
         - Description : 构造函数
         - Variables : void
         *******************************/
        linkThread(Handler handler){
            this.handler = handler;
        }

        public void run(){
            try {
                DatagramSocket socket = new DatagramSocket(LINK_PORT);

                // 持续监听
                while (true) {

                    // 收包
                    byte[] buf = new byte[1024];
                    DatagramPacket packet = new DatagramPacket(buf, buf.length);
                    socket.receive(packet);
                    buf = packet.getData();
                    data = new String(buf, 0, packet.getLength());


                    // 解析并修改UI
                    new Thread(new Runnable(){
                        @Override
                        public void run() {
                            Message msg =new Message();
                            msg.what = 0;
                            msg.obj = data;
                            handler.sendMessage(msg);
                        }

                    }).start();

                }
            } catch (SocketException e) {
                e.printStackTrace();
            } catch (IOException e) {
                e.printStackTrace();
            }
        }


    }


    /******************************************************************
     *
     * 数据下载定时申请线程
     *
     */
    public class dataLoadThread extends Thread{

        private final int STOP_TIME = 5000;   // 间歇时间
        private int count = 0;                // 启动时间，为0时不申请

        public void run(){
            super.run();
            while(true){
                if(count == 0){
                    count++;
                    continue;
                }
                try {
                    Thread.sleep(STOP_TIME);
                    callDataApply();
                } catch (InterruptedException e) {
                    e.printStackTrace();
                }
            }

        }

    }
}
