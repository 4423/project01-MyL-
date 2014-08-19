﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Manage_your_Life
{
    /// <summary>
    /// 時間軸の使用時間ViewModel
    /// </summary>
    public class OneDayProcessStatus : ViewModel
    {

        public OneDayProcessStatus()
        {
            Items = new List<MyItem>();

            Items.Add(new MyItem()
            {
                Key = DateTime.Today,
                Value = 0
            });
        }

        /// <summary>
        /// ある１日の中のプロセスの使用タイミングと使用時間をもつListを作成
        /// </summary>
        /// <param name="appId">取得したいプロセスのID</param>
        /// <param name="day">取得したい日付</param>
        public OneDayProcessStatus(int appId, DateTime day, bool isStacked)
        {
            DatabaseOperation dbOperator = DatabaseOperation.Instance;
            var database = dbOperator.GetConnectionedDataContext;

            var q = (
                    from p in database.DatabaseApplication
                    where p.DatabaseTimeline.Today == day
                    where p.Id == appId
                    select new
                    {
                        Key = p.DatabaseTimeline.Now,
                        Value = Utility.ToRoundDown((TimeSpan.Parse(p.DatabaseTimeline.UsageTime)).TotalMinutes, 3)
                    });

            double stack = 0;
            Items = new List<MyItem>();
            foreach (var r in q)
            {
                //積み上げグラフの場合
                if (isStacked)
                {
                    stack += r.Value;
                    Items.Add(new MyItem()
                    {
                        Key = (DateTime)r.Key,
                        Value = stack
                    });
                }
                else
                {
                    Items.Add(new MyItem()
                    {
                        Key = (DateTime)r.Key,
                        Value = r.Value
                    });
                }
            }
        }


        public List<MyItem> Items { get; set; }
        public class MyItem
        {
            public DateTime Key { get; set; }
            public double Value { get; set; }
        }

    }
}
