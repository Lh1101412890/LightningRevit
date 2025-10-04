using System;

using Autodesk.Revit.UI;

namespace LightningRevit.LightningExtension
{
    /// <summary>
    /// Revit外部事件处理器，必须在主线程中构造
    /// </summary>
    public class ExternalEventHandler : IExternalEventHandler
    {
        /// <summary>
        /// 事件执行
        /// </summary>
        protected Action<UIApplication> Action;
        /// <summary>
        /// 事件名称
        /// </summary>
        protected string EventName;

        private readonly ExternalEvent HandlerEvent;
        public ExternalEventHandler(Action<UIApplication> action, string name)
        {
            Action = action;
            EventName = name;
            HandlerEvent = ExternalEvent.Create(this);
        }
        /// <summary>
        /// 触发事件
        /// </summary>
        public void Raise()
        {
            HandlerEvent.Raise();
        }

        public void Dispose()
        {
            HandlerEvent.Dispose();
            Dispose();
        }

        public void Execute(UIApplication app)
        {
            Action.Invoke(app);
        }

        public string GetName()
        {
            return EventName;
        }
    }
}
