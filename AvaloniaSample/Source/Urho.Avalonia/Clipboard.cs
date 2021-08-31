using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Urho.Gui;

namespace Urho.Avalonia
{
    class ClipboardImpl : IClipboard
    {

        private Urho.Gui.UI UI;
        public ClipboardImpl()
        {
            UI = Application.Current.UI;
        }
        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public Task<object> GetDataAsync(string format)
        {
            throw new NotImplementedException();
        }

        public Task<string[]> GetFormatsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTextAsync()
        {
            return Task.FromResult(UI.ClipboardText);
        }

        public Task SetDataObjectAsync(IDataObject data)
        {
            throw new NotImplementedException();
        }

        public Task SetTextAsync(string text)
        {
            if (text != null)
                UI.ClipboardText = text;

            return Task.CompletedTask;
        }
    }

}

