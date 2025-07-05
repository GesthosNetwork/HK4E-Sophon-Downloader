namespace Sophon.Helper
{
    public delegate void DelegateWriteStreamInfo(long writeBytes);
    public delegate void DelegateWriteDownloadInfo(long downloadedBytes, long diskWriteBytes);
    public delegate void DelegateDownloadAssetComplete(SophonAsset asset);
}