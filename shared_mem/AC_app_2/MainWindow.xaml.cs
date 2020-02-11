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
using System.Globalization;

namespace AC_app_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AssettoCorsa acSession;
        SerialPort _port;
        string pitchPosCommand = "02503A2B{0}{1}{2}03";    //STX P : + <3 digits> ETX
        string pitchNegCommand = "02503A2D{0}{1}{2}03";    //STX P : - <3 digits> ETX
        string rollPosCommand = "02523A2B{0}{1}{2}03";     //STX R : + <3 digits> ETX
        string rollNegCommand = "02523A2D{0}{1}{2}03";     //STX R : - <3 digits> ETX
        string zeroCommand = "025A03";                     //STX Z ETX
        string stopCommand = "025303";                     //STX S ETX
        float pitch;
        float roll;
        AC_STATUS gameStat;

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;
            string[] portNames = SerialPort.GetPortNames();
            portCombo.ItemsSource = portNames;
            statusBar.Text = "Motion settings can not be changed during a race";
            portCombo.IsEditable = false;
            motionSlider.IsEnabled = false;
            disconnectBtn.IsEnabled = false;
            scaleBox.IsEnabled = false;

            pitch = 0;
            roll = 0;
            gameStat = AC_STATUS.AC_OFF;

            acSession = new AssettoCorsa();
            acSession.PhysicsInterval = 500;        // these are in milliseconds
            acSession.GraphicsInterval = 10000;
            acSession.StaticInfoInterval = 5000;
            acSession.PhysicsUpdated += AC_PhysicsUpdated; // Add event listener for StaticInfo
            acSession.GameStatusChanged += AC_GameStatusChanged;
            acSession.Start(); // Connect to shared memory and start interval timers 
            Console.Read();
            
        }


        private void AC_PhysicsUpdated(object sender, PhysicsEventArgs e)
        {
            //Console.WriteLine("Pitch = " + e.Physics.Pitch + "°, Roll = " + e.Physics.Roll + "°");
            string pitchForm;
            string rollForm;
            float pitch = e.Physics.Pitch * (180 / Convert.ToSingle(Math.PI));
            float roll = e.Physics.Roll * (180 / Convert.ToSingle(Math.PI));
            Graphics graphicsData = acSession.ReadGraphics();
            Console.WriteLine(graphicsData.Status);

            if (pitch >= 0)
            {
                pitchForm = pitchPosCommand;
            }
            else
            {
                pitchForm = pitchNegCommand;
            }

            if (roll >= 0)
            {
                rollForm = rollPosCommand;
            }
            else
            {
                rollForm = rollNegCommand;
            }

            string pitchStr = pitch.ToString("00.0");
            string rollStr = roll.ToString("00.0");

            string pitchStrTrim = pitchStr.Replace("-", "");
            string rollStrTrim = rollStr.Replace("-", "");

            string fullPitch = String.Format(pitchForm, Convert.ToByte(pitchStrTrim[0]).ToString("X2"), Convert.ToByte(pitchStrTrim[1]).ToString("X2"), Convert.ToByte(pitchStrTrim[3]).ToString("X2"));
            string fullRoll = String.Format(rollForm, Convert.ToByte(rollStrTrim[0]).ToString("X2"), Convert.ToByte(rollStrTrim[1]).ToString("X2"), Convert.ToByte(rollStrTrim[3]).ToString("X2"));
            
            try
            {
                if (_port.IsOpen)
                {
                    Console.WriteLine("Pitch = " + pitchStr + "°, Roll = " + rollStr + "° \n");
                    Console.WriteLine("accel = " + e.Physics.SpeedKmh); //this is ~0 when in menu and pits but paused is constant
                    //Console.WriteLine("pitch: " + fullPitch);
                    //Console.WriteLine("roll: " + fullRoll + "\n");
                    _port.Write(fullPitch);
                    _port.Write(fullRoll);
                }   
            }
            catch (Exception ex)
            {
                Console.WriteLine("error writing to port: " + ex.Message);
            }
        }


        private void AC_GameStatusChanged(object sender, EventArgs e)
        {
            Console.WriteLine("status changed = " + e.ToString() + "\n");
        }


        private void connectBtn_Click(object sender, RoutedEventArgs e)
        {
            connectBtn.IsEnabled = false;
            disconnectBtn.IsEnabled = true;
            portCombo.IsEnabled = false;
            try
            {
                _port = new SerialPort(portCombo.Text);
                _port.Open();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "cannot connect to " + portCombo.Text, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }


        private void disconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _port.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connectBtn.IsEnabled = true;
                disconnectBtn.IsEnabled = false;
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


        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (_port != null && _port.IsOpen)
            {
                _port.Close();
            }
        }
    }
}
