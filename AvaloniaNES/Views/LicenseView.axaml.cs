using Avalonia.Controls;
using System.IO;

namespace AvaloniaNES.Views;

public partial class LicenseView : Window
{
    public LicenseView()
    {
        InitializeComponent();
        if (File.Exists("LICENSE"))
        {
            m_Edit_License.Text = File.ReadAllText("LICENSE");
        }
        else
        {
            m_Edit_License.Text = "Apache License 2.0";
        }
    }
}