﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
//using System.Data.Objects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using System.IO;
using System.Windows.Interop;
using System.Windows.Controls.DataVisualization;
using System.Windows.Controls.DataVisualization.Charting;
using FirstFloor.ModernUI.Windows.Controls;

namespace Manage_your_Life
{

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        #region メンバ
        /// <summary>
        /// WPFでタイマーを使う
        /// </summary>
        DispatcherTimer timer;

        /// <summary>
        /// 前回のProcess
        /// </summary>
        Process previousProcess;

        /// <summary>
        /// WinAPIを叩くProcessInformationクラスのインスタンス
        /// </summary>
        ProcessInformation pInfo;

        /// <summary>
        /// 最初に最前面になった時の日時
        /// </summary>
        DateTime firstActiveDate;

        /// <summary>
        /// データベースを操作
        /// </summary>
        DatabaseOperation dbOperator;

        /// <summary>
        /// 登録アプリ同士の計測スルーバグ回避用
        /// </summary>
        bool preTitleCheck = false; //TODO 改名したいけど何やってるのか分からない

        /// <summary>
        /// アプリケーションが最前面から外れた時の検出
        /// false: 最前面
        /// true: 背面(最前面から外れた初回)
        /// </summary>
        bool isRearApplication = false;

        /// <summary>
        /// バルーン通知
        /// </summary>
        private NotifyIcon notifyIcon;

        #endregion


        public MainWindow()
        {
            InitializeComponent();

            pInfo = new ProcessInformation();
            dbOperator = DatabaseOperation.Instance;       

            //バルーン通知の設定
            notifyIcon = new NotifyIcon();
            notifyIcon.Text = "Manage your Life";
            notifyIcon.Icon = Properties.Resources.taskTrayIcon;
            notifyIcon.Visible = true;

            //タイマーの作成
            timer = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher);
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += new EventHandler(DispatcherTimer_Tick);
            //タイマーの実行開始
            timer.Start();
        }


        /// <summary>
        /// タイマー指定時間が経過すると呼び出される
        /// </summary>
        /// <see cref="http://ari-it.doorblog.jp/archives/28684231.html"/>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            //アクティブプロセス取得
            Process activeProcess = pInfo.GetActiveProcess();

            //初回起動などnullだったら現在のプロセスを代入
            if (previousProcess == null) previousProcess = activeProcess;

            //前回と同じプロセス名だったら何もしない
            if ((activeProcess.ProcessName == previousProcess.ProcessName) && !preTitleCheck)
            {
                timer.Start();
                return;
            }

            //最前面のアプリケーションが変わった時にしたい処理
            ApplicationChanged(activeProcess);
            
            //キャッシュ
            previousProcess = activeProcess;
            timer.Start();
        }



        /// <summary>
        /// 最前面のアプリケーションが変わった時にする処理
        /// </summary>
        /// <param name="activeProcess">新たな最前面のProcess</param>
        private void ApplicationChanged(Process activeProcess)
        {
            //最初に最前面になった時
            if (!isRearApplication)
            {
                try
                {
                    //DBに存在していなければ新規にデータ挿入
                    if (!dbOperator.IsExist(activeProcess))
                    {
                        dbOperator.Register(activeProcess);
                    }
                }
                catch (Exception ex)
                {
                    notifyIcon.ShowBalloonTip(500, "Error",
                        ex.Message + "\n画面の遷移に処理が追いつきませんでした。", ToolTipIcon.Error);
                }

                //最初にアクティブになった時間を取得
                firstActiveDate = DateTime.Now;

                isRearApplication = true;
                preTitleCheck = false;
            }
            else //最前面解除
            {
                //計測時間追記の為にDBから該当Idを取得
                //CAUTION プロセス終了の例外発生
                try
                {
                    int appId = dbOperator.GetCorrespondingAppId(previousProcess.MainModule.FileName);

                    //DBから使用時間を取得し、今回の使用時間を加算してDB更新
                    var activeInterval = Utility.GetInterval(firstActiveDate);
                    dbOperator.UpdateUsageTime(appId, activeInterval);

                    //バルーンで通知
                    ShowBalloonTip(activeInterval);

                }
                catch (Exception ex)
                {
                    notifyIcon.ShowBalloonTip(500, "Error",
                        ex.Message + "\n画面の遷移に処理が追いつきませんでした。", ToolTipIcon.Error);
                }

                isRearApplication = false;
                preTitleCheck = true;
            }
            
        }



        /// <summary>
        /// 今まで最前面にあったプロセスの使用時間を通知する
        /// </summary>
        /// <param name="activeInterval">今回の使用時間</param>
        private void ShowBalloonTip(TimeSpan activeInterval)
        {
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon.BalloonTipTitle = "\"" + previousProcess.ProcessName + "\"" + "の計測終了";
            notifyIcon.BalloonTipText = "使用時間: " + activeInterval.ToString(@"hh\:mm\:ss");
            notifyIcon.ShowBalloonTip(1000);
        }



        private void ModernWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            timer.Stop();
            this.Hide();

            TodayReport window = new TodayReport();
            window.ShowDialog();


        }
        

    }      
}