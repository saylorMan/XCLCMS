﻿using System;
using System.Web;
using XCLCMS.IService.Logger;

namespace XCLCMS.Service.Logger
{
    public class LogService : ILogService
    {
        static LogService()
        {
            //XCLNetLogger配置信息
            XCLNetLogger.Config.LogConfig.SetConfig(HttpContext.Current.Server.MapPath("~/Config/XCLNetLogger.config"));
        }

        public void WriteLog(LogModel model)
        {
            XCLNetLogger.Log.WriteLog(new XCLNetLogger.Model.LogModel()
            {
                ClientIP = model.ClientIP,
                Code = model.Code,
                Contents = model.Contents,
                CreateTime = model.CreateTime,
                LogLevel = (XCLNetLogger.Config.LogConfig.LogLevel)((int)model.LogLevel),
                LogType = model.LogType,
                RefferUrl = model.RefferUrl,
                Remark = model.Remark,
                Title = model.Title,
                Url = model.Url
            });
        }

        public void WriteLog(Exception ex, string remark = null)
        {
            XCLNetLogger.Log.WriteLog(ex, remark);
        }

        public void WriteLog(LogEnum.LogLevel logLevel, string title, string contents = null)
        {
            XCLNetLogger.Log.WriteLog((XCLNetLogger.Config.LogConfig.LogLevel)((int)logLevel), title, contents);
        }
    }
}