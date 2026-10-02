using System;
using System.Collections.Generic;
using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public interface IRecentFilesService
    {
        event EventHandler? RecentFilesChanged;
        IReadOnlyList<RecentFile> GetRecentFiles();
        void AddRecentFile(string filePath, string operationType);
        void RemoveRecentFile(string filePath);
        void ClearRecentFiles();
    }
}
