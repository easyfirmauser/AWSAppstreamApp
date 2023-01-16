using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Amazon.AppStream.Model;
using Amazon.CloudWatch.Model;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Grid;
using AWSAppstreamApp.APIs;
using Stack = Amazon.AppStream.Model.Stack;

namespace AWSAppstreamApp
{
    //<copyright file="MainWindow.cs" company="WoAx-IT Wolfgang Axamit KG">
    // WoAx-IT Wolfgang Axamit KG. All rights reserved.
    // </copyright>  
    public partial class MainWindow
    {
        public const string DockLayoutManagerXml = "DockLayoutManager.xml";
        public const string DgStacksXml = "DgStacks.xml";
        public const string DgFleetXml = "DgFleets.xml";
        public const string DgUsersXml = "DgUsers.xml";
        public const string DgUserAssocXml = "DgUserAssoc.xml";
        private const string LayoutSettingsFolderName = "LayoutSettings";
        public string AccessKeyID => TbApiKeyID.Text;
        public string AccessKeySecret => TbApiSecret.Text;
        public bool ResetLayoutHappened;
        public static string AppDataSessionFolder { get; set; } = System.AppDomain.CurrentDomain.BaseDirectory + "\\S3SessionsLog";
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void SaveLayout()
        {
            if (!ResetLayoutHappened)
            {
                var stackXml = GetPath(DgStacksXml);
                DgStacks.SaveLayoutToXml(stackXml);
                DgFleets.SaveLayoutToXml(GetPath(DgFleetXml));
                DgUsers.SaveLayoutToXml(GetPath(DgUsersXml));
                DgUserStackAassocations.SaveLayoutToXml(GetPath(DgUserAssocXml));
                DockLayoutManager.SaveLayoutToXml(GetPath(DockLayoutManagerXml));
                SaveSubGrids(DgStacks);
                SaveSubGrids(DgFleets);
            }
        }

        private void SaveSubGrids(GridControl pGrid)
        {
            foreach (var vDesc in ((TabViewDetailDescriptor)pGrid.DetailDescriptor).DetailDescriptors)
            {
                var grid = ((DataControlDetailDescriptor)vDesc).DataControl as GridControl;
                grid.SaveLayoutToXml(GetPath(grid.Name + ".xml"));
            }
        }

        private void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ReadSavedAPIData();
            if (ReInitApis())
            {
                ReloadData();
                InitPeriodsAndStats();
            }

            InitDate();
        }

        private void ReadSavedAPIData()
        {
            TbApiKeyID.Text = AppsettingsHelper.ReadSetting("AccessKeyID");
            TbApiSecret.Text = AppsettingsHelper.ReadSetting("AccessKeySecret");
            TbBucketName.Text = AppsettingsHelper.ReadSetting("BucketName");
        }

        private void InitDate()
        {
            tbDatumFrom.DateTime = DateTime.Now.AddDays(-20);
            tbDatumTo.DateTime = DateTime.Now;
            tbDatumS3ApiFrom.DateTime = DateTime.Now.AddDays(-20);
            tbDatumS3ApiTo.DateTime = DateTime.Now;
        }

        private void InitPeriodsAndStats()
        {
            var periods = GetPeriods();
            LbPeriods.ItemsSource = periods;
            LbPeriods.SelectedItem = periods.Skip(1).First();

            LbStats.ItemsSource = GetStats();
            LbStats.SelectedItem = "Average";
        }

        private List<Period> GetPeriods()
        {
            return new List<Period>()
            {
                new Period() { Name = "1 sec", Value = 1 },
                new Period() { Name = "5 sec", Value = 5 },
                new Period() { Name = "10 sec", Value = 10 },
                new Period() { Name = "30 sec", Value = 30 },
                new Period() { Name = "1 min", Value = 60 },
                new Period() { Name = "5 min", Value = 300 },
                new Period() { Name = "10 min", Value = 600 },
                new Period() { Name = "30 min", Value = 1800 },
                new Period() { Name = "1 hour", Value = 3600 }
            };
        }

        private List<string> GetStats()
        {
            //https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/Statistics-definitions.html
            return new List<string>()
            {
                "SampleCount",
                "Sum",
                "Average",
                "Minimum",
                "Maximum",
                //"Percentile",
                //"Trimmed mean",
                //"Interquartile mean",
                //"Winsorized mean",
                //"Percentile rank",
                //"Trimmed count",
                //"Trimmed sum",
            };
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveLayout();
            SaveData();
        }

        private void SaveData()
        {
            AppsettingsHelper.AddUpdateAppSettings("BucketName", TbBucketName.Text);

            //if (!String.IsNullOrWhiteSpace(TbBucketName.Text))
            //{
            //    AppsettingsHelper.AddUpdateAppSettings("BucketName", TbBucketName.Text);
            //}
        }
        
        private void RestoreLayout()
        {
            var vStacksPath = GetPath(DgStacksXml);
            if (File.Exists(vStacksPath))
            {
                DgStacks.RestoreLayoutFromXml(vStacksPath);
            }

            var vFleetsPath = GetPath(DgFleetXml);
            if (File.Exists(vFleetsPath))
            {
                DgFleets.RestoreLayoutFromXml(vFleetsPath);
            }

            var vUsersXml = GetPath(DgUsersXml);
            if (File.Exists(vUsersXml))
            {
                DgUsers.RestoreLayoutFromXml(vUsersXml);
            }

            var vUserAssocXml = GetPath(DgUserAssocXml);
            if (File.Exists(vUserAssocXml))
            {
                DgUserStackAassocations.RestoreLayoutFromXml(vUserAssocXml);
            }

            var vDocklayoutXml = GetPath(DockLayoutManagerXml);
            if (File.Exists(vDocklayoutXml))
            {
                DockLayoutManager.RestoreLayoutFromXml(vDocklayoutXml);
            }

        }

        private static string GetPath(string pXmlPath)
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var vLayoutSettingsPath = Path.Combine(dir, LayoutSettingsFolderName);
            Directory.CreateDirectory(vLayoutSettingsPath);
            return Path.Combine(dir, pXmlPath);
        }

        private void BtnDeleteOutput(object sender, System.Windows.RoutedEventArgs e)
        {
            ClearOutput();
        }
        private void BtnDeleteUser(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                User vSelectedUser = GetSelectedUser();
                if (vSelectedUser != null)
                {
                    var vRes = AppstreamAPI.DeleteUser(vSelectedUser.UserName);
                    WriteOutput(vRes);
                    ReloadData();
                }

            }
            catch (Exception vException)
            {
                WriteOutput(vException.Message);
            }

        }
        private void BtnDisableUser(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                User vSelectedUser = GetSelectedUser();
                if (vSelectedUser != null)
                {
                    var vRes = AppstreamAPI.DisableUser(vSelectedUser.UserName);
                    WriteOutput(vRes);
                    ReloadData();
                }
            }
            catch (Exception vException)
            {
                WriteOutput(vException.Message);
            }
        }
        private void BtnEnableUser(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                User vSelectedUser = GetSelectedUser();
                if (vSelectedUser != null)
                {
                    var vRes = AppstreamAPI.EnableUser(vSelectedUser.UserName);
                    WriteOutput(vRes);
                    ReloadData();
                }
            }
            catch (Exception vException)
            {
                WriteOutput(vException.Message);
            }
        }
        private void BtnCreateUser(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(TbUsername.Text) &&
                    !String.IsNullOrWhiteSpace(TbFirstName.Text) &&
                    !String.IsNullOrWhiteSpace(TbLastName.Text))
                {
                    var vRes = AppstreamAPI.CreateUser(TbFirstName.Text, TbLastName.Text, TbUsername.Text);
                    WriteOutput(vRes);
                    ReloadData();
                }
            }
            catch (Exception vException)
            {
                WriteOutput(vException.Message);
            }
        }
        private void BtnUserToStack(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                User vSelectedUser = GetSelectedUser();
                var vSelectedStack = CbStacks.SelectedItem as Stack;
                if (vSelectedUser != null && vSelectedStack != null)
                {
                    var vRes = AppstreamAPI.UserToStack(vSelectedUser.UserName, vSelectedStack.Name);
                    WriteOutput(vRes);
                    ReloadData();
                }
            }
            catch (Exception vException)
            {
                WriteOutput(vException.Message);
            }
        }

        private void BtnDeleteUserStack(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                //User vSelectedUser = GetSelectedUser();
                //var vSelectedStack = CbStacks.SelectedItem as Stack;
                var vSelectedAssocItem = DgUserStackAassocations.SelectedItem as UserStackAssociation;
                if (vSelectedAssocItem != null)
                {
                    var vRes = AppstreamAPI.DeleteUserToStack(vSelectedAssocItem.UserName,
                        vSelectedAssocItem.StackName);
                    WriteOutput(vRes);
                    ReloadData();
                }
            }
            catch (Exception vException)
            {
                WriteOutput(vException.Message);
            }
        }
        private void BtnChangeApiKeys(object sender, System.Windows.RoutedEventArgs e)
        {
            ClearOutput();
            if (SaveAPIData())
            {
                ReadSavedAPIData();
                if (ReInitApis())
                {
                    ReloadData();
                    InitPeriodsAndStats();
                }
            }
        }

        private bool SaveAPIData()
        {
            bool vIsSuccessfull = false;
            if (!String.IsNullOrWhiteSpace(TbApiKeyID.Text) &&
                !String.IsNullOrWhiteSpace(TbApiSecret.Text))
            {
                AppsettingsHelper.AddUpdateAppSettings("AccessKeyID", TbApiKeyID.Text);
                AppsettingsHelper.AddUpdateAppSettings("AccessKeySecret", TbApiSecret.Text);
                vIsSuccessfull = true;
            }
            else
            {
                MessageBox.Show("Please fill up AccessKeyID and AccessKeySecret!");
            }
            return vIsSuccessfull;
        }

        private bool ReInitApis()
        {
            var res = false;

            if (!String.IsNullOrWhiteSpace(AccessKeyID) &&
                !String.IsNullOrWhiteSpace(AccessKeySecret))
            {
                AppstreamAPI.Init(AccessKeyID, AccessKeySecret);
                CloudWatchAPI.Init(AccessKeyID, AccessKeySecret);
                S3API.Init(AccessKeyID, AccessKeySecret);
                res = true;
            }

            return res;
        }

        private async void ReloadData()
        {
            try
            {
                pbStatus.IsIndeterminate = true;

                DgStacks.ItemsSource = null;
                DgFleets.ItemsSource = null;
                DgUsers.ItemsSource = null;
                DgUserStackAassocations.ItemsSource = null;
                //DgMetrics.ItemsSource = null;
                await Task.Delay(500);
                List<Stack> vStacks = await AppstreamAPI.GetStacks();
                DgStacks.ItemsSource = vStacks;
                CbStacks.ItemsSource = vStacks;

                if (vStacks.Any() && vStacks.Count == 1)
                {
                    CbStacks.SelectedItem = vStacks.First();
                }

                var vFleets = await AppstreamAPI.GetFleets();
                DgFleets.ItemsSource = vFleets;
                DgUsers.ItemsSource = await AppstreamAPI.GetUsers();

                var vAssoc = await AppstreamAPI.GetUserStackAssociations(vStacks);
                DgUserStackAassocations.ItemsSource = vAssoc;

                //DgMetrics.ItemsSource = await CloudWatchAPI.GetMetrics();
                LbMetrics.ItemsSource = await CloudWatchAPI.GetMetrics();
            }
            catch (Exception e)
            {
                WriteOutput(e.Message);
            }
            finally
            {
                pbStatus.IsIndeterminate = false;
            }
           
        }
        private User GetSelectedUser()
        {
            return DgUsers.SelectedItem as User;
        }

        public void WriteOutput(string pMessage)
        {
            var vMsg = pMessage;
            if (TbOutput.Text.Any())
            {
                vMsg = vMsg.Insert(0, System.Environment.NewLine);
            }

            TbOutput.Text += vMsg;
            TbOutput.ScrollToEnd();
        }
        private void ClearOutput()
        {
            TbOutput.Text = "";
        }

        private void BtnReload(object sender, System.Windows.RoutedEventArgs e)
        {
            ReloadData();
        }

        private void DgStacks_OnMouseDoubleClick(object pSender, MouseButtonEventArgs pE)
        {
            try
            {
                if (DgStacks?.SelectedItem is Stack vCurrentStack)
                {
                    JsonViewerWindow vWindow = new JsonViewerWindow(vCurrentStack);
                    vWindow.ShowDialog();
                }

            }
            catch (Exception ex)
            {
                WriteOutput(ex.Message);
            }
        }

        private void DgFleets_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (DgFleets?.SelectedItem is Fleet vCurrentStack)
                {
                    JsonViewerWindow vWindow = new JsonViewerWindow(vCurrentStack);
                    vWindow.ShowDialog();
                }

            }
            catch (Exception ex)
            {
                WriteOutput(ex.Message);
            }
        }

        private void DgUsers_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (DgUsers?.SelectedItem is User vUser)
                {
                    JsonViewerWindow vWindow = new JsonViewerWindow(vUser);
                    vWindow.ShowDialog();
                }

            }
            catch (Exception ex)
            {
                WriteOutput(ex.Message);
            }
        }

        private void DgStacks_OnAutoGeneratedColumns(object pSender, RoutedEventArgs pE)
        {
            var vDetailDescriptors = ((TabViewDetailDescriptor)DgStacks.DetailDescriptor).DetailDescriptors;
            vDetailDescriptors?.Clear();
            var vDescriptors = GetDetailDescriptors(new Stack());

            if (vDescriptors != null)
                foreach (var vDesc in vDescriptors)
                {
                    vDetailDescriptors?.Add(vDesc);
                }

            RestoreLayout();
            //foreach (var vDgStacksColumn in DgStacks.Columns)
            //{
            //    if (vDgStacksColumn.FieldName == nameof(Stack.ApplicationSettings))
            //    {
            //        vDgStacksColumn.CellTemplate = this.Resources["AppSettingsTemplate"] as DataTemplate;
            //    }
            //}
        }


        private void DgFleet_OnAutoGeneratedColumns(object pSender, RoutedEventArgs pE)
        {
            var vDetailDescriptors = ((TabViewDetailDescriptor)DgFleets.DetailDescriptor).DetailDescriptors;
            vDetailDescriptors?.Clear();

            var vDescriptors = GetDetailDescriptors(new Fleet());

            if (vDescriptors != null)
                foreach (var vDesc in vDescriptors)
                {
                    vDetailDescriptors.Add(vDesc);
                }

            RestoreLayout();
        }

        private IEnumerable<DetailDescriptorBase> GetDetailDescriptors(object pObject)
        {
            List<DataControlDetailDescriptor> resultList = new List<DataControlDetailDescriptor>();
            var vType = pObject.GetType();
            foreach (PropertyInfo prop in vType.GetProperties())
            {
                if (prop.PropertyType != typeof(string) && typeof(IEnumerable<object>).IsAssignableFrom(prop.PropertyType))
                {
                    var vNewSub = MakeDetailDescriptor(prop.Name, vType.Name);
                    resultList.Add(vNewSub);
                }
            }

            return resultList;
        }

        private static DataControlDetailDescriptor MakeDetailDescriptor(string pPropName, string pTypeName)
        {
            var vNewSub = new DataControlDetailDescriptor();
            vNewSub.ItemsSourceBinding = new Binding(pPropName);
            vNewSub.ShowHeader = true;

            GridControl vGc = new GridControl()
            {
                Name = pTypeName + "_" + pPropName,
                AutoGenerateColumns = AutoGenerateColumnsMode.AddNew,
                View = new TableView()
                {
                    VerticalScrollbarVisibility = ScrollBarVisibility.Auto,
                    NavigationStyle = GridViewNavigationStyle.Row,
                    DetailHeaderContent = pPropName,
                    ShowGroupPanel = false,
                    AllowEditing = false,
                    EnableSelectedRowAppearance = true
                },
            };
            vGc.AutoGeneratedColumns += VGc_AutoGeneratedColumns;
            vNewSub.DataControl = vGc;
            return vNewSub;
        }

        private static void VGc_AutoGeneratedColumns(object sender, RoutedEventArgs e)
        {
            var vGrid = sender as GridControl;
            var vPath = GetPath(vGrid.Name + ".xml");
            if (File.Exists(vPath))
            {
                vGrid.RestoreLayoutFromXml(vPath);
            }
        }

        private void DgUsers_OnAutoGeneratedColumns(object pSender, RoutedEventArgs pE)
        {
            RestoreLayout();
        }

        private void BtnResetLayout(object pSender, RoutedEventArgs pE)
        {
            try
            {
                var vLayoutFolderPath = GetPath("");
                if (vLayoutFolderPath != null)
                {
                    Directory.Delete(vLayoutFolderPath, true);
                    MessageBox.Show("Please restart app!");
                    WriteOutput("Folder deleted!");
                    ResetLayoutHappened = true;
                }
            }
            catch (Exception ex)
            {
                WriteOutput(ex.Message);
            }
        }

        private async void BtnGo(object pSender, RoutedEventArgs pE)
        {
            try
            {
                pbStatus.IsIndeterminate = true;
                ClearOutput();

                XYDiagram2D diagram2D = new XYDiagram2D
                {
                    AxisX = new AxisX2D()
                    {
                        Label = new AxisLabel() { TextPattern = "{A:dd/MM/yyyy HH:mm}" },
                        DateTimeScaleOptions = new ContinuousDateTimeScaleOptions()
                        {
                            GridAlignment = DateTimeGridAlignment.Minute,
                        },
                    },
                    AxisY = new AxisY2D()
                    {
                        WholeRange = new DevExpress.Xpf.Charts.Range()
                        {
                            MinValue = 0
                        }
                    }
                };
                diagram2D.AxisY.WholeRange.SetValue(AxisY2D.AlwaysShowZeroLevelProperty, true);

                ChartControlMetrics.Diagram = diagram2D;

                var selectedMetrics = LbMetrics.SelectedItems.Cast<Metric>().ToList();
                if (selectedMetrics.Any())
                {
                    var selectedPeriod = LbPeriods.SelectedItem as Period;
                    var selectedStat = LbStats.SelectedItem as string;
                    var vDateFrom = tbDatumFrom.DateTime;
                    var vDateTo = tbDatumTo.DateTime;
                    var vData = await CloudWatchAPI.GetDataForMetricsAsync(selectedMetrics,
                        selectedPeriod, selectedStat, vDateFrom, vDateTo);

                    //lineSeries2D.ArgumentScaleType = ScaleType.DateTime;
                    //lineSeries2D.LabelsVisibility = true;

                    StringBuilder sb = new StringBuilder();
                    foreach (var vMetricDataResult in vData)
                    {
                        var lineSeries2D = new LineSeries2D
                        {
                            CrosshairLabelPattern = "{S}: {A:dd/MM/yyyy H:mm}: {V:n2}",
                            DisplayName = vMetricDataResult.Id,
                            AnimationAutoStartMode = AnimationAutoStartMode.PlayOnce
                        };

                        sb.AppendLine(vMetricDataResult.Id);
                        for (int i = 0; i < vMetricDataResult.Timestamps.Count; i++)
                        {
                            var vSeriespoint = new SeriesPoint(vMetricDataResult.Timestamps[i],
                                vMetricDataResult.Values[i]);
                            lineSeries2D.Points.Add(vSeriespoint);
                            sb.AppendLine(vMetricDataResult.Timestamps[i] + ": " + vMetricDataResult.Values[i]);
                        }

                        diagram2D.Series.Add(lineSeries2D);
                    }

                    WriteOutput(sb.ToString());
                }


            }
            catch (Exception ex)
            {
                WriteOutput(ex.Message);
            }
            finally
            {
                pbStatus.IsIndeterminate = false;
            }
        }

        private async void BtnGetS3Stat(object sender, RoutedEventArgs e)
        {
            try
            {
                pbStatus.IsIndeterminate = true;
                var vBucket = TbBucketName.Text;
                var dateTimeStart = DateTime.Now.AddDays(-20);
                var dateTimeEnd = DateTime.Now;
                var list=await S3API.GetObject(dateTimeStart, dateTimeEnd, WriteOutput, vBucket, AppDataSessionFolder);
                DgS3Sessions.ItemsSource = list;
            }
            catch (Exception exception)
            {
                WriteOutput(exception.Message);
            }
            finally
            {
                pbStatus.IsIndeterminate = false;
            }
        }
    }
}
