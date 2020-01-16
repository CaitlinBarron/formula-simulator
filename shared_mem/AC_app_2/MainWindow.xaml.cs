using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using AssettoCorsaSharedMemory;

namespace AC_app_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            string[] portNames = SerialPort.GetPortNames();
            portCombo.ItemsSource = portNames;
            statusBar.Text = "Motion settings can not be changed during a race";
            portCombo.IsEditable = false;
            motionSlider.IsEnabled = false;
            disconnectBtn.IsEnabled = false;
            scaleBox.IsEnabled = false;

            AssettoCorsa ac = new AssettoCorsa();
            ac.PhysicsInterval = 10;
            ac.GraphicsInterval = 10000;
            ac.StaticInfoInterval = 5000;
            ac.PhysicsUpdated += AC_PhysicsUpdated; // Add event listener for StaticInfo
            ac.GraphicsUpdated += AC_GraphicsUpdated;
            ac.Start(); // Connect to shared memory and start interval timers 
            Console.Read();
        }

        static void AC_PhysicsUpdated(object sender, PhysicsEventArgs e)
        {
            Console.WriteLine("Pitch = " + e.Physics.Pitch + ", Roll = " + e.Physics.Roll);
        }

        static void AC_GraphicsUpdated(object sender, GraphicsEventArgs e)
        {
            if (e.Graphics.Status == AC_STATUS.AC_PAUSE)
            {
                Console.WriteLine("Paused");
            }
        }

        private void connectBtn_Click(object sender, RoutedEventArgs e)
        {
            connectBtn.IsEnabled = false;
            disconnectBtn.IsEnabled = true;
            portCombo.IsEnabled = false;
            try
            {
                SerialPort port = new SerialPort(portCombo.Text);
                port.Open();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void disconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            connectBtn.IsEnabled = true;
            disconnectBtn.IsEnabled = false;
            try
            {
                SerialPort port = new SerialPort(portCombo.Text);
                port.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                portCombo.IsEnabled = true;
            }
        }

        private void scaleCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (scaleCheck.IsChecked == true)
            {
                motionSlider.IsEnabled = true;
                scaleBox.IsEnabled = true;
            }
            else
            {
                motionSlider.IsEnabled = false;
                scaleBox.IsEnabled = false;
            }
        }

        private void motionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scaleBox.Text = Math.Round((decimal)motionSlider.Value, 0).ToString();
        }

        private void scaleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int currNum = (int) motionSlider.Value;
            int val = 0;
            bool isNum = int.TryParse(scaleBox.Text, out val);
            if(isNum == true)
            {
                if(val >= 100)
                {
                    scaleBox.Text = "100";
                    motionSlider.Value = 100;
                }
                else if(val <= 0)
                {
                    scaleBox.Text = "0";
                    motionSlider.Value = 0;
                }
                else
                {
                    motionSlider.Value = val;
                }
            }
            else
            {
                scaleBox.Text = currNum.ToString();
            }
        }
    }
}
