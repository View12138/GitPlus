using System.Drawing.Design;

namespace GitPlus.Commons;

/// <summary>Custom UITypeEditor that provides a file-browse button for the VS options property grid.</summary>
public sealed class FileBrowserEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        => UITypeEditorEditStyle.Modal;

    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select git.exe",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (dialog.ShowDialog() == true)
            return dialog.FileName;

        return value;
    }
}
