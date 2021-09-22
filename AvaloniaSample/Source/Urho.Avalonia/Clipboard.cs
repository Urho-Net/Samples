using System;
using System.Collections.Generic;
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
            UI.UseSystemClipboard = true;
        }
        public Task ClearAsync()
        {
            return SetTextAsync("");
        }

        public async Task<object> GetDataAsync(string format)
        {
             if (format == DataFormats.Text)
                return await GetTextAsync();

            // TBD ELI , should it be implemented ?
            if (format == DataFormats.FileNames)
                throw new NotImplementedException();
                
            throw new NotImplementedException();
        }

        public Task<string[]> GetFormatsAsync()
        {

            var rv = new List<string>();
            rv.Add(DataFormats.Text);
            return Task.FromResult(rv.ToArray());
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

