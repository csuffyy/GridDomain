﻿using System;
using Microsoft.Owin.Hosting;
using Quartz;


namespace GridDomain.Scheduling.WebUI
{
    public class WebUiWrapper : IWebUiWrapper
    {
        public class WebUiWrapperDispose : IDisposable
        {
            private readonly IDisposable _webAppDisposable;

            public WebUiWrapperDispose(IDisposable webAppDisposable)
            {
                _webAppDisposable = webAppDisposable;
            }

            public void Dispose()
            {
                _webAppDisposable.Dispose();
            }
        }

        private readonly IWebUiConfig _webUiConfig;
        private readonly IScheduler _scheduler;
        private IDisposable _wrapperDispose;

        public WebUiWrapper(
            IWebUiConfig webUiConfig,
            IScheduler scheduler
            )
        {
            _webUiConfig = webUiConfig;
            _scheduler = scheduler;
        }

        public IDisposable Start()
        {
            var webAppDispose = WebApp.Start(_webUiConfig.Url, app => app.Use(QuartzNetWebConsole.Setup.Owin("/", () => _scheduler)));
            _wrapperDispose = new WebUiWrapperDispose(webAppDispose);
            return _wrapperDispose;
        }

        public void Dispose()
        {
            _wrapperDispose.Dispose();
        }
    }
}
