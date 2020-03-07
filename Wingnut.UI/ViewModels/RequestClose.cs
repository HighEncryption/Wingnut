namespace Wingnut.UI.ViewModels
{
    using System;

    public delegate void RequestCloseEventHandler(object sender, RequestCloseEventArgs e);

    public class RequestCloseEventArgs : EventArgs
    {
        public RequestCloseEventArgs()
        {
        }

        public RequestCloseEventArgs(bool dialogResult)
        {
            this.DialogResult = dialogResult;
        }

        public bool? DialogResult { get; set; }
    }
}