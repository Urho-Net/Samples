using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Platform.Interop;

namespace  Urho.Avalonia
{
    class SystemDialogImp : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, Window parent)
        {
            // throw new NotImplementedException();
            return  Task.FromResult((string[])null);
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent)
        {
            // throw new NotImplementedException();
            return Task.FromResult((string)null);
        }
    }
}