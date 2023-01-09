using System.Windows;
using Newtonsoft.Json;

namespace AWSAppstreamApp
{
    /// <summary>
    /// Interaction logic for JsonViewer.xaml
    /// </summary>
    public partial class JsonViewerWindow : Window
    {
        public object obj;
        public JsonViewerWindow(
            object pObject)
        {
            obj = pObject;
            InitializeComponent();
            var vObjectToJson = JsonConvert.SerializeObject(obj);
            JsonViewerStacks.Load(vObjectToJson);
            //JsonViewerStacks.ExpandAll();
        }
    }
}
