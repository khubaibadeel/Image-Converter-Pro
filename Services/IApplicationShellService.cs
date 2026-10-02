namespace ImageConverterPro.Services
{
    public interface IApplicationShellService
    {
        bool ConfirmExitDuringConversion();
        void RequestExit();
        void ShowAbout();
        void OpenWebsite();
    }
}
